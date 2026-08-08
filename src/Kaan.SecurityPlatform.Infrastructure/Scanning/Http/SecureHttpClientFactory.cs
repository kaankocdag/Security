using System.Net;
using System.Net.Sockets;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Http;

/// <summary>
/// Pasif tarayıcının kullanacağı güvenli HTTP client üreticisi.
/// SocketsHttpHandler.ConnectCallback ile her bağlantı öncesi
/// çözümlenen IP adresini SSRF validator ile denetler.
/// </summary>
public sealed class SecureHttpClientFactory : IDisposable
{
    private readonly ITargetSafetyValidator _safety;
    private readonly ILogger<SecureHttpClientFactory> _logger;

    public SecureHttpClientFactory(
        ITargetSafetyValidator safety,
        ILogger<SecureHttpClientFactory> logger)
    {
        _safety = safety;
        _logger = logger;
    }

    public HttpClient Create(TimeSpan? timeout = null, int maxRedirects = 5, bool allowRedirects = true)
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = allowRedirects,
            MaxAutomaticRedirections = maxRedirects,
            AutomaticDecompression = DecompressionMethods.All,
            UseCookies = false,
            ConnectTimeout = TimeSpan.FromSeconds(8),
            PooledConnectionLifetime = TimeSpan.FromMinutes(1)
        };

        handler.ConnectCallback = async (context, ct) =>
        {
            var host = context.DnsEndPoint.Host;
            var check = _safety.ValidateHost(host);
            if (!check.IsSafe)
            {
                _logger.LogWarning("SSRF koruması ConnectCallback: {Host} reddedildi ({Reason})", host, check.ReasonCode);
                throw new HttpRequestException($"Hedef güvensiz: {check.ReasonCode} - {check.Detail}");
            }

            IPAddress[] addresses;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(host, ct);
            }
            catch (SocketException ex)
            {
                // "Bilinen böyle bir ana bilgisayar yok" — debugger'da unhandled crash olmasın.
                _logger.LogWarning(ex, "DNS çözülemedi: {Host}", host);
                throw new HttpRequestException(
                    $"DNS çözülemedi: '{host}' bulunamadı (SocketError={ex.SocketErrorCode}). Wildcard/geçersiz host veya ağ DNS sorunu olabilir.",
                    ex);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Geçersiz host: {Host}", host);
                throw new HttpRequestException($"Geçersiz host: '{host}'.", ex);
            }

            if (addresses.Length == 0)
            {
                throw new HttpRequestException($"DNS boş döndü: {host}");
            }

            foreach (var address in addresses)
            {
                var ipCheck = _safety.ValidateResolvedIp(address);
                if (!ipCheck.IsSafe)
                {
                    _logger.LogWarning("SSRF koruması ConnectCallback IP: {Ip} reddedildi ({Reason})", address, ipCheck.ReasonCode);
                    throw new HttpRequestException($"Hedef IP güvensiz: {ipCheck.ReasonCode} - {ipCheck.Detail}");
                }
            }

            var safeAddress = addresses.FirstOrDefault(a =>
                a.AddressFamily == AddressFamily.InterNetwork ||
                a.AddressFamily == AddressFamily.InterNetworkV6);
            if (safeAddress is null)
            {
                throw new HttpRequestException($"Hedef için uygun IP çözümlenemedi: {host}");
            }

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(safeAddress, context.DnsEndPoint.Port, ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException ex)
            {
                socket.Dispose();
                throw new HttpRequestException($"TCP bağlantısı kurulamadı: {host}:{context.DnsEndPoint.Port}", ex);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        };

        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(15)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("KaanSecurityScanner/1.0 (+https://kaansecurity.local)");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("tr-TR,tr;q=0.9,en;q=0.8");

        return client;
    }

    public void Dispose()
    {
    }
}
