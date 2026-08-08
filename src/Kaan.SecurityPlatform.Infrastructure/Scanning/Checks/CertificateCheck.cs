using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Checks;

public sealed class CertificateCheck : IPassiveSecurityCheck
{
    private readonly ITargetSafetyValidator _safety;
    private readonly ILogger<CertificateCheck> _logger;

    public CertificateCheck(ITargetSafetyValidator safety, ILogger<CertificateCheck> logger)
    {
        _safety = safety;
        _logger = logger;
    }

    public string CheckCode => "https.certificate";
    public string DisplayName => "TLS Sertifika Kontrolü";
    public string Category => "Transport Security";
    public ScanType SupportedScanTypes => ScanType.FullPassive | ScanType.Certificate;
    public int Order => 12;

    public async Task<CheckOutcome> RunAsync(ScanContext context, CancellationToken cancellationToken = default)
    {
        var findings = new List<CheckFinding>();

        if (!string.Equals(context.TargetUri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            return new CheckOutcome(CheckCode, CheckStatus.Skipped, findings, "HTTP hedefinde TLS sertifika kontrolü uygulanmaz.");
        }

        var check = _safety.ValidateHost(context.NormalizedHostName);
        if (!check.IsSafe)
        {
            return new CheckOutcome(CheckCode, CheckStatus.Skipped, findings, check.Detail);
        }

        var port = context.TargetUri.IsDefaultPort ? 443 : context.TargetUri.Port;
        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(context.NormalizedHostName, port, cancellationToken);
            using var netStream = tcp.GetStream();
            using var ssl = new SslStream(netStream, false,
                (_, _, _, _) => true);

            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = context.NormalizedHostName,
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.None
            }, cancellationToken);

            if (ssl.RemoteCertificate is not X509Certificate2 cert)
            {
                findings.Add(new CheckFinding(
                    Title: "Sertifika alınamadı",
                    Description: "TLS anlaşması sırasında uzak sertifika okunamadı.",
                    Severity: Severity.Medium,
                    Confidence: ConfidenceLevel.Recommendation,
                    Category: "Transport Security",
                    AffectedUrl: context.TargetUri.ToString(),
                    Fingerprint: "tls.cert.unavailable"));
                return new CheckOutcome(CheckCode, CheckStatus.IssuesFound, findings);
            }

            var daysUntilExpiry = (cert.NotAfter.ToUniversalTime() - DateTime.UtcNow).TotalDays;
            if (daysUntilExpiry < 0)
            {
                findings.Add(new CheckFinding(
                    Title: "TLS sertifikası süresi dolmuş",
                    Description: $"Sunucu sertifikasının süresi {Math.Abs(daysUntilExpiry):F0} gün önce doldu. Kullanıcılar tarayıcılarında güvenlik uyarısı görecek.",
                    Severity: Severity.Critical,
                    Confidence: ConfidenceLevel.Confirmed,
                    Category: "Transport Security",
                    CweCode: "CWE-295",
                    AffectedUrl: context.TargetUri.ToString(),
                    Evidence: $"NotAfter: {cert.NotAfter:u}",
                    Remediation: "Sertifikayı yenileyin (örn. Let's Encrypt) ve web sunucusunda kurun.",
                    Fingerprint: "tls.cert.expired"));
            }
            else if (daysUntilExpiry < 15)
            {
                findings.Add(new CheckFinding(
                    Title: "TLS sertifikası kısa sürede sona erecek",
                    Description: $"Sunucu sertifikasının süresi {daysUntilExpiry:F0} gün içinde dolacak.",
                    Severity: Severity.High,
                    Confidence: ConfidenceLevel.Confirmed,
                    Category: "Transport Security",
                    AffectedUrl: context.TargetUri.ToString(),
                    Evidence: $"NotAfter: {cert.NotAfter:u}",
                    Remediation: "Sertifikayı süresinin dolmasından önce yenileyin.",
                    Fingerprint: "tls.cert.expiring"));
            }
            else if (daysUntilExpiry < 30)
            {
                findings.Add(new CheckFinding(
                    Title: "TLS sertifikası 30 gün içinde sona erecek",
                    Description: $"Sunucu sertifikasının süresi {daysUntilExpiry:F0} gün içinde dolacak. Otomatik yenileme yapıldığından emin olun.",
                    Severity: Severity.Medium,
                    Confidence: ConfidenceLevel.Confirmed,
                    Category: "Transport Security",
                    AffectedUrl: context.TargetUri.ToString(),
                    Remediation: "Otomatik sertifika yenileme (ACME/certbot) aktif edin.",
                    Fingerprint: "tls.cert.short-window"));
            }

            var signatureAlg = cert.SignatureAlgorithm.FriendlyName ?? cert.SignatureAlgorithm.Value ?? string.Empty;
            if (signatureAlg.Contains("sha1", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new CheckFinding(
                    Title: "SHA-1 imzalı sertifika",
                    Description: "Sertifika zayıf SHA-1 algoritması ile imzalanmış. SHA-1 modern tarayıcılar tarafından güvensiz kabul edilir.",
                    Severity: Severity.High,
                    Confidence: ConfidenceLevel.Confirmed,
                    Category: "Transport Security",
                    CweCode: "CWE-327",
                    AffectedUrl: context.TargetUri.ToString(),
                    Evidence: signatureAlg,
                    Remediation: "Sertifikayı SHA-256 veya daha güçlü bir algoritma ile yeniden düzenleyin.",
                    Fingerprint: "tls.cert.sha1"));
            }

            var subjectHost = context.NormalizedHostName;
            var altNames = cert.Extensions
                .OfType<X509SubjectAlternativeNameExtension>()
                .SelectMany(ext => ext.EnumerateDnsNames())
                .ToArray();
            var matchesHost = altNames.Any(name => MatchesHost(name, subjectHost))
                              || MatchesHost(cert.GetNameInfo(X509NameType.SimpleName, false), subjectHost);
            if (!matchesHost)
            {
                findings.Add(new CheckFinding(
                    Title: "Sertifika host eşleşmesi başarısız",
                    Description: "Sertifika içindeki isim(ler) taranan host ile eşleşmiyor. Tarayıcılar 'name mismatch' uyarısı üretecek.",
                    Severity: Severity.High,
                    Confidence: ConfidenceLevel.Confirmed,
                    Category: "Transport Security",
                    AffectedUrl: context.TargetUri.ToString(),
                    Evidence: $"SubjectAltNames: {string.Join(", ", altNames)}",
                    Remediation: "Domain'i içeren yeni bir sertifika edinin.",
                    Fingerprint: "tls.cert.name-mismatch"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Sertifika kontrolü sırasında hata: {Host}", context.NormalizedHostName);
            return new CheckOutcome(CheckCode, CheckStatus.Skipped, findings, ex.Message);
        }

        return new CheckOutcome(CheckCode, findings.Count == 0 ? CheckStatus.Passed : CheckStatus.IssuesFound, findings);
    }

    private static bool MatchesHost(string? certName, string host)
    {
        if (string.IsNullOrWhiteSpace(certName))
        {
            return false;
        }
        certName = certName.Trim().TrimEnd('.').ToLowerInvariant();
        host = host.Trim().TrimEnd('.').ToLowerInvariant();

        if (certName == host)
        {
            return true;
        }

        if (certName.StartsWith("*."))
        {
            var suffix = certName[1..];
            return host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                && host.Count(c => c == '.') == certName.Count(c => c == '.');
        }

        return false;
    }
}
