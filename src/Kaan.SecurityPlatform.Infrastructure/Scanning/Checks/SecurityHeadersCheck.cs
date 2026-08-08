using System.Net.Http.Headers;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Checks;

/// <summary>
/// HTTPS bağlantısında dönen güvenlik başlıklarını tek istek ile toplu kontrol eder.
/// HSTS, CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy,
/// Permissions-Policy ve X-XSS-Protection başlıklarını inceler.
/// </summary>
public sealed class SecurityHeadersCheck : IPassiveSecurityCheck
{
    private readonly SecureHttpClientFactory _httpFactory;
    private readonly ILogger<SecurityHeadersCheck> _logger;

    public SecurityHeadersCheck(SecureHttpClientFactory httpFactory, ILogger<SecurityHeadersCheck> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public string CheckCode => "http.security-headers";
    public string DisplayName => "Güvenlik Başlıkları";
    public string Category => "Security Headers";
    public ScanType SupportedScanTypes => ScanType.FullPassive | ScanType.SecurityHeaders;
    public int Order => 20;

    public async Task<CheckOutcome> RunAsync(ScanContext context, CancellationToken cancellationToken = default)
    {
        var findings = new List<CheckFinding>();
        using var client = _httpFactory.Create(timeout: TimeSpan.FromSeconds(12), allowRedirects: true, maxRedirects: 5);

        try
        {
            using var response = await client.GetAsync(context.TargetUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var headers = MergeHeaders(response.Headers, response.Content.Headers);

            EvaluateHsts(context, headers, findings);
            EvaluateCsp(context, headers, findings);
            EvaluateXContentTypeOptions(context, headers, findings);
            EvaluateXFrameOptions(context, headers, findings);
            EvaluateReferrerPolicy(context, headers, findings);
            EvaluatePermissionsPolicy(context, headers, findings);
            EvaluateServerHeader(context, headers, findings);
            EvaluateXPoweredBy(context, headers, findings);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Güvenlik başlığı kontrolü isteği başarısız: {Uri}", context.TargetUri);
            return new CheckOutcome(CheckCode, CheckStatus.Skipped, findings, ex.Message);
        }

        var status = findings.Count == 0 ? CheckStatus.Passed : CheckStatus.IssuesFound;
        return new CheckOutcome(CheckCode, status, findings);
    }

    private static IReadOnlyDictionary<string, string> MergeHeaders(HttpResponseHeaders responseHeaders, HttpContentHeaders contentHeaders)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in responseHeaders)
        {
            dict[header.Key] = string.Join(", ", header.Value);
        }
        foreach (var header in contentHeaders)
        {
            dict[header.Key] = string.Join(", ", header.Value);
        }
        return dict;
    }

    private static void EvaluateHsts(ScanContext context, IReadOnlyDictionary<string, string> headers, List<CheckFinding> findings)
    {
        if (!string.Equals(context.TargetUri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!headers.TryGetValue("Strict-Transport-Security", out var value) || string.IsNullOrWhiteSpace(value))
        {
            findings.Add(new CheckFinding(
                Title: "HSTS başlığı yok",
                Description: "HTTPS servisi Strict-Transport-Security başlığı göndermiyor. Kullanıcılar aktif olarak HTTPS'ye yönlendirilmediği sürece SSL stripping saldırılarına açık kalır.",
                Severity: Severity.High,
                Confidence: ConfidenceLevel.Confirmed,
                Category: "Security Headers",
                CweCode: "CWE-319",
                OwaspCategory: "A05:2021 - Security Misconfiguration",
                AffectedUrl: context.TargetUri.ToString(),
                Remediation: "'Strict-Transport-Security: max-age=63072000; includeSubDomains; preload' başlığını tüm HTTPS yanıtlara ekleyin.",
                RemediationExampleConfig: "add_header Strict-Transport-Security \"max-age=63072000; includeSubDomains; preload\" always;",
                TurkishExecutiveSummary: "Sitenizin HTTPS oturumlarında güvenlik zorunluluğu bildirilmiyor. HSTS eklenerek tarayıcıların HTTPS'de kalması sağlanmalı.",
                Fingerprint: "sh.hsts.missing"));
            return;
        }

        if (!value.Contains("max-age", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new CheckFinding(
                Title: "HSTS başlığı geçersiz",
                Description: "Strict-Transport-Security başlığı gönderiliyor fakat max-age direktifi bulunmuyor. Bu durum başlığın etkisiz kalmasına neden olur.",
                Severity: Severity.Medium,
                Confidence: ConfidenceLevel.Confirmed,
                Category: "Security Headers",
                AffectedUrl: context.TargetUri.ToString(),
                Evidence: value,
                Remediation: "'max-age=63072000; includeSubDomains; preload' değerini kullanın.",
                Fingerprint: "sh.hsts.invalid"));
        }
    }

    private static void EvaluateCsp(ScanContext context, IReadOnlyDictionary<string, string> headers, List<CheckFinding> findings)
    {
        if (!headers.TryGetValue("Content-Security-Policy", out var csp) || string.IsNullOrWhiteSpace(csp))
        {
            findings.Add(new CheckFinding(
                Title: "Content-Security-Policy başlığı yok",
                Description: "Content-Security-Policy (CSP) başlığı bulunmuyor. XSS ve veri sızıntısı gibi tarayıcı tabanlı saldırılara karşı güçlü bir savunma katmanı devre dışı.",
                Severity: Severity.Medium,
                Confidence: ConfidenceLevel.Confirmed,
                Category: "Security Headers",
                CweCode: "CWE-1021",
                OwaspCategory: "A05:2021 - Security Misconfiguration",
                AffectedUrl: context.TargetUri.ToString(),
                Remediation: "İçeriğinize uygun bir CSP tanımlayın. 'default-src \\'self\\'' ile başlayıp gereken kaynakları ekleyin.",
                RemediationExampleConfig: "add_header Content-Security-Policy \"default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self';\" always;",
                TurkishExecutiveSummary: "Tarayıcının çalıştırabileceği kaynak kısıtlaması yok. XSS gibi saldırılara karşı ek koruma sağlanmalı.",
                Fingerprint: "sh.csp.missing"));
            return;
        }

        if (csp.Contains("unsafe-inline", StringComparison.OrdinalIgnoreCase) || csp.Contains("unsafe-eval", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new CheckFinding(
                Title: "CSP zayıf tanımlanmış",
                Description: "CSP tanımı 'unsafe-inline' veya 'unsafe-eval' direktifi içeriyor. Bu direktifler modern XSS korumalarını zayıflatır.",
                Severity: Severity.Low,
                Confidence: ConfidenceLevel.StrongIndication,
                Category: "Security Headers",
                AffectedUrl: context.TargetUri.ToString(),
                Evidence: csp,
                Remediation: "'unsafe-inline' ve 'unsafe-eval' yerine nonce veya hash tabanlı kural kullanın.",
                Fingerprint: "sh.csp.weak"));
        }
    }

    private static void EvaluateXContentTypeOptions(ScanContext context, IReadOnlyDictionary<string, string> headers, List<CheckFinding> findings)
    {
        if (!headers.TryGetValue("X-Content-Type-Options", out var value)
            || !value.Contains("nosniff", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new CheckFinding(
                Title: "X-Content-Type-Options: nosniff eksik",
                Description: "Tarayıcının MIME tipini tahmin etmesini önleyen 'nosniff' başlığı gönderilmiyor.",
                Severity: Severity.Low,
                Confidence: ConfidenceLevel.Confirmed,
                Category: "Security Headers",
                AffectedUrl: context.TargetUri.ToString(),
                Remediation: "'X-Content-Type-Options: nosniff' başlığını ekleyin.",
                Fingerprint: "sh.nosniff.missing"));
        }
    }

    private static void EvaluateXFrameOptions(ScanContext context, IReadOnlyDictionary<string, string> headers, List<CheckFinding> findings)
    {
        var hasXFrame = headers.TryGetValue("X-Frame-Options", out _);
        var hasFrameAncestors = headers.TryGetValue("Content-Security-Policy", out var csp)
            && csp.Contains("frame-ancestors", StringComparison.OrdinalIgnoreCase);
        if (!hasXFrame && !hasFrameAncestors)
        {
            findings.Add(new CheckFinding(
                Title: "Clickjacking koruması yok",
                Description: "X-Frame-Options veya CSP frame-ancestors başlığı yok. Sitenin başka bir sayfada iframe içine gömülmesi engellenmiyor.",
                Severity: Severity.Medium,
                Confidence: ConfidenceLevel.Confirmed,
                Category: "Security Headers",
                CweCode: "CWE-1021",
                AffectedUrl: context.TargetUri.ToString(),
                Remediation: "'X-Frame-Options: DENY' veya CSP 'frame-ancestors \\'none\\'' direktifi ekleyin.",
                Fingerprint: "sh.clickjacking.missing"));
        }
    }

    private static void EvaluateReferrerPolicy(ScanContext context, IReadOnlyDictionary<string, string> headers, List<CheckFinding> findings)
    {
        if (!headers.TryGetValue("Referrer-Policy", out var _))
        {
            findings.Add(new CheckFinding(
                Title: "Referrer-Policy başlığı yok",
                Description: "Referrer-Policy tanımlanmadığı için tarayıcı default politikasını kullanır ve harici sitelere gizlilik açısından hassas bilgi gönderebilir.",
                Severity: Severity.Informational,
                Confidence: ConfidenceLevel.Recommendation,
                Category: "Security Headers",
                AffectedUrl: context.TargetUri.ToString(),
                Remediation: "'Referrer-Policy: strict-origin-when-cross-origin' başlığı ekleyin.",
                Fingerprint: "sh.referrer.missing"));
        }
    }

    private static void EvaluatePermissionsPolicy(ScanContext context, IReadOnlyDictionary<string, string> headers, List<CheckFinding> findings)
    {
        if (!headers.TryGetValue("Permissions-Policy", out var _))
        {
            findings.Add(new CheckFinding(
                Title: "Permissions-Policy başlığı yok",
                Description: "Kamera, mikrofon, konum gibi hassas tarayıcı özelliklerine erişim kısıtlanmamış.",
                Severity: Severity.Informational,
                Confidence: ConfidenceLevel.Recommendation,
                Category: "Security Headers",
                AffectedUrl: context.TargetUri.ToString(),
                Remediation: "Kullanmadığınız özellikleri kapatın. Örn: 'Permissions-Policy: geolocation=(), microphone=(), camera=()'",
                Fingerprint: "sh.permissions.missing"));
        }
    }

    private static void EvaluateServerHeader(ScanContext context, IReadOnlyDictionary<string, string> headers, List<CheckFinding> findings)
    {
        if (headers.TryGetValue("Server", out var server) && !string.IsNullOrWhiteSpace(server))
        {
            var hasVersion = System.Text.RegularExpressions.Regex.IsMatch(server, "\\d+\\.\\d+");
            if (hasVersion)
            {
                findings.Add(new CheckFinding(
                    Title: "Sunucu sürüm bilgisi ifşa ediliyor",
                    Description: "'Server' başlığı sunucu türü ve sürüm bilgisi açığa çıkarıyor. Sürüm bilgisi hedefli saldırılara yardımcı olur.",
                    Severity: Severity.Low,
                    Confidence: ConfidenceLevel.StrongIndication,
                    Category: "Information Disclosure",
                    CweCode: "CWE-200",
                    AffectedUrl: context.TargetUri.ToString(),
                    Evidence: $"Server: {server}",
                    Remediation: "Sunucu sürüm bilgisini gizleyin. Nginx: 'server_tokens off;', Apache: 'ServerTokens Prod'.",
                    Fingerprint: "info.server.version"));
            }
        }
    }

    private static void EvaluateXPoweredBy(ScanContext context, IReadOnlyDictionary<string, string> headers, List<CheckFinding> findings)
    {
        if (headers.TryGetValue("X-Powered-By", out var value) && !string.IsNullOrWhiteSpace(value))
        {
            findings.Add(new CheckFinding(
                Title: "X-Powered-By başlığı bilgi ifşa ediyor",
                Description: "'X-Powered-By' başlığı kullanılan teknoloji hakkında bilgi veriyor.",
                Severity: Severity.Informational,
                Confidence: ConfidenceLevel.StrongIndication,
                Category: "Information Disclosure",
                CweCode: "CWE-200",
                AffectedUrl: context.TargetUri.ToString(),
                Evidence: $"X-Powered-By: {value}",
                Remediation: "'X-Powered-By' başlığını kapatın. Nginx/Apache/uygulama sunucusunda ilgili ayarı devre dışı bırakın.",
                Fingerprint: "info.xpoweredby"));
        }
    }
}
