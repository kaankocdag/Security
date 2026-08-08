using System.Net;
using System.Net.Sockets;
using DnsClient;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Safety;

public sealed class TargetSafetyValidator : ITargetSafetyValidator
{
    private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http", "https"
    };

    private static readonly HashSet<string> ForbiddenHostSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "localdomain",
        "metadata.google.internal",
        "metadata.internal",
        "instance-data",
        "169.254.169.254",
        "kubernetes.default.svc"
    };

    private readonly ILogger<TargetSafetyValidator> _logger;
    private readonly ILookupClient _dns;

    public TargetSafetyValidator(ILogger<TargetSafetyValidator> logger, ILookupClient dns)
    {
        _logger = logger;
        _dns = dns;
    }

    public TargetSafetyResult ValidateUri(Uri uri)
    {
        if (uri is null)
        {
            return TargetSafetyResult.Unsafe("null_uri", "Hedef URI null olamaz.");
        }

        if (!AllowedSchemes.Contains(uri.Scheme))
        {
            return TargetSafetyResult.Unsafe("forbidden_scheme", $"'{uri.Scheme}' şeması tarama için desteklenmiyor. Sadece http/https izinlidir.");
        }

        return ValidateHost(uri.Host);
    }

    public TargetSafetyResult ValidateHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return TargetSafetyResult.Unsafe("empty_host", "Hedef host boş olamaz.");
        }

        var normalized = host.Trim().TrimEnd('.').ToLowerInvariant();

        if (normalized.Contains('*') || normalized.StartsWith('.') || normalized.Contains(' '))
        {
            return TargetSafetyResult.Unsafe(
                "wildcard_or_invalid_host",
                $"'{host}' taranamaz. Wildcard (*) veya geçersiz host DNS'te çözülemez; somut bir hostname seçin.");
        }

        foreach (var suffix in ForbiddenHostSuffixes)
        {
            if (normalized == suffix || normalized.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase))
            {
                return TargetSafetyResult.Unsafe("forbidden_host", $"'{host}' hostuna tarama gönderilemez.");
            }
        }

        if (IPAddress.TryParse(normalized, out var literalIp))
        {
            return ValidateResolvedIp(literalIp);
        }

        return TargetSafetyResult.Safe();
    }

    public TargetSafetyResult ValidateResolvedIp(IPAddress address)
    {
        if (address is null)
        {
            return TargetSafetyResult.Unsafe("null_ip", "IP adresi çözümlenemedi.");
        }

        if (IPAddress.IsLoopback(address))
        {
            return TargetSafetyResult.Unsafe("loopback_ip", $"Loopback IP tarama için izinli değil: {address}");
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();

            // 10.0.0.0/8
            if (bytes[0] == 10) return Unsafe(address, "private_10");
            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return Unsafe(address, "private_172");
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168) return Unsafe(address, "private_192");
            // 127.0.0.0/8
            if (bytes[0] == 127) return Unsafe(address, "loopback_range");
            // 169.254.0.0/16 link local / metadata
            if (bytes[0] == 169 && bytes[1] == 254) return Unsafe(address, "link_local");
            // 100.64.0.0/10 CGNAT
            if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) return Unsafe(address, "cgnat");
            // 0.0.0.0/8
            if (bytes[0] == 0) return Unsafe(address, "unspecified");
            // 224.0.0.0/4 multicast
            if (bytes[0] >= 224 && bytes[0] <= 239) return Unsafe(address, "multicast");
            // 240.0.0.0/4 reserved
            if (bytes[0] >= 240) return Unsafe(address, "reserved");
        }
        else if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
            {
                return Unsafe(address, "ipv6_local");
            }

            if (address.Equals(IPAddress.IPv6Loopback) || address.Equals(IPAddress.IPv6Any))
            {
                return Unsafe(address, "ipv6_loopback");
            }

            var v6Bytes = address.GetAddressBytes();
            // fc00::/7 unique local
            if ((v6Bytes[0] & 0xFE) == 0xFC)
            {
                return Unsafe(address, "ipv6_ula");
            }
        }

        return TargetSafetyResult.Safe();
    }

    public async Task<TargetSafetyResult> ValidateAndResolveAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        var initial = ValidateUri(uri);
        if (!initial.IsSafe)
        {
            return initial;
        }

        if (IPAddress.TryParse(uri.Host, out _))
        {
            return initial;
        }

        try
        {
            var dnsResponse = await _dns.QueryAsync(uri.Host, QueryType.A, cancellationToken: cancellationToken);
            foreach (var record in dnsResponse.Answers.ARecords())
            {
                var check = ValidateResolvedIp(record.Address);
                if (!check.IsSafe)
                {
                    _logger.LogWarning("SSRF koruması: {Host} -> {Ip} reddedildi ({Reason})",
                        uri.Host, record.Address, check.ReasonCode);
                    return check;
                }
            }

            var dnsResponseV6 = await _dns.QueryAsync(uri.Host, QueryType.AAAA, cancellationToken: cancellationToken);
            foreach (var record in dnsResponseV6.Answers.AaaaRecords())
            {
                var check = ValidateResolvedIp(record.Address);
                if (!check.IsSafe)
                {
                    _logger.LogWarning("SSRF koruması: {Host} -> {Ip} reddedildi ({Reason})",
                        uri.Host, record.Address, check.ReasonCode);
                    return check;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DNS çözümlemesi başarısız: {Host}", uri.Host);
            return TargetSafetyResult.Unsafe("dns_failure", $"DNS çözümlenemedi: {ex.Message}");
        }

        return TargetSafetyResult.Safe();
    }

    private static TargetSafetyResult Unsafe(IPAddress address, string reason) =>
        TargetSafetyResult.Unsafe($"unsafe_{reason}", $"Tehlikeli IP adresi ({reason}): {address}");
}
