using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Checks;

public sealed class CookieSecurityCheck : IPassiveSecurityCheck
{
    private readonly SecureHttpClientFactory _httpFactory;
    private readonly ILogger<CookieSecurityCheck> _logger;

    public CookieSecurityCheck(SecureHttpClientFactory httpFactory, ILogger<CookieSecurityCheck> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public string CheckCode => "http.cookie-security";
    public string DisplayName => "Çerez Güvenliği";
    public string Category => "Cookie Security";
    public ScanType SupportedScanTypes => ScanType.FullPassive | ScanType.Cookie;
    public int Order => 30;

    public async Task<CheckOutcome> RunAsync(ScanContext context, CancellationToken cancellationToken = default)
    {
        var findings = new List<CheckFinding>();
        using var client = _httpFactory.Create(timeout: TimeSpan.FromSeconds(10));

        try
        {
            using var response = await client.GetAsync(context.TargetUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                return new CheckOutcome(CheckCode, CheckStatus.Passed, findings, "Cevapta çerez ayarlanmadı.");
            }

            foreach (var cookie in setCookies)
            {
                var lower = cookie.ToLowerInvariant();
                var cookieName = cookie.Split('=')[0];
                var missing = new List<string>();
                if (!lower.Contains("secure"))
                {
                    missing.Add("Secure");
                }
                if (!lower.Contains("httponly"))
                {
                    missing.Add("HttpOnly");
                }
                if (!lower.Contains("samesite"))
                {
                    missing.Add("SameSite");
                }

                if (missing.Count > 0)
                {
                    var severity = missing.Contains("Secure") || missing.Contains("HttpOnly")
                        ? Severity.Medium
                        : Severity.Low;

                    findings.Add(new CheckFinding(
                        Title: $"Çerez '{cookieName}' güvenlik bayrakları eksik",
                        Description: $"Set-Cookie yanıtında {string.Join(", ", missing)} bayrağı bulunmuyor.",
                        Severity: severity,
                        Confidence: ConfidenceLevel.Confirmed,
                        Category: "Cookie Security",
                        CweCode: "CWE-614",
                        AffectedUrl: context.TargetUri.ToString(),
                        AffectedParameter: cookieName,
                        Evidence: cookie,
                        Remediation: "Çerezlere en az 'Secure; HttpOnly; SameSite=Lax' bayraklarını ekleyin. Cross-origin çerezler için SameSite=None + Secure kullanın.",
                        TurkishExecutiveSummary: $"'{cookieName}' çerezi tarayıcıda güvensiz saklanıyor. Bayraklar eklenmezse XSS veya CSRF saldırılarında oturum çalınabilir.",
                        Fingerprint: $"cookie.flags.{cookieName}"));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Cookie kontrolü sırasında istek başarısız");
            return new CheckOutcome(CheckCode, CheckStatus.Skipped, findings, ex.Message);
        }

        return new CheckOutcome(CheckCode, findings.Count == 0 ? CheckStatus.Passed : CheckStatus.IssuesFound, findings);
    }
}
