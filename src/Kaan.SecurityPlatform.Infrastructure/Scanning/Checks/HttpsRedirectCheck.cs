using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Checks;

public sealed class HttpsRedirectCheck : IPassiveSecurityCheck
{
    private readonly SecureHttpClientFactory _httpFactory;
    private readonly ILogger<HttpsRedirectCheck> _logger;

    public HttpsRedirectCheck(SecureHttpClientFactory httpFactory, ILogger<HttpsRedirectCheck> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public string CheckCode => "http.https-redirect";
    public string DisplayName => "HTTP → HTTPS Yönlendirme";
    public string Category => "Transport Security";
    public ScanType SupportedScanTypes => ScanType.FullPassive | ScanType.PassiveWeb;
    public int Order => 12;

    public async Task<CheckOutcome> RunAsync(ScanContext context, CancellationToken cancellationToken = default)
    {
        var httpUri = new UriBuilder(context.TargetUri) { Scheme = "http", Port = 80 }.Uri;

        try
        {
            using var client = _httpFactory.Create(timeout: TimeSpan.FromSeconds(8), allowRedirects: false);
            using var response = await client.GetAsync(httpUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var status = (int)response.StatusCode;
            if (status is >= 300 and < 400 && response.Headers.Location is { } location)
            {
                if (string.Equals(location.Scheme, "https", StringComparison.OrdinalIgnoreCase))
                {
                    return new CheckOutcome(CheckCode, CheckStatus.Passed, Array.Empty<CheckFinding>());
                }

                return new CheckOutcome(CheckCode, CheckStatus.IssuesFound, new[]
                {
                    new CheckFinding(
                        Title: "HTTP isteği HTTPS'e yönlendirilmiyor",
                        Description: "HTTP portundan gelen istek HTTPS yerine tekrar HTTP hedefine yönlendiriliyor.",
                        Severity: Severity.High,
                        Confidence: ConfidenceLevel.Confirmed,
                        Category: Category,
                        CweCode: "CWE-311",
                        OwaspCategory: "A02:2021 - Cryptographic Failures",
                        AffectedUrl: httpUri.ToString(),
                        Evidence: $"Location: {location}",
                        Remediation: "Web sunucusunda HTTP → HTTPS için 301 kalıcı yönlendirme yapılandırın.",
                        RemediationExampleConfig: "return 301 https://$host$request_uri;",
                        Fingerprint: "https.redirect.non-secure")
                });
            }

            if (status is >= 200 and < 300)
            {
                return new CheckOutcome(CheckCode, CheckStatus.IssuesFound, new[]
                {
                    new CheckFinding(
                        Title: "HTTP üzerinden düz metin içerik dönüyor",
                        Description: "HTTP portu 200 yanıt döndürüyor. Site HTTPS'e zorlanmıyor ve içerik şifrelenmemiş biçimde erişilebilir.",
                        Severity: Severity.High,
                        Confidence: ConfidenceLevel.Confirmed,
                        Category: Category,
                        CweCode: "CWE-311",
                        AffectedUrl: httpUri.ToString(),
                        Remediation: "HTTP dinleyicisinde tüm istekleri HTTPS'e 301 yönlendirmesi ile zorunlu kılın.",
                        Fingerprint: "https.redirect.plain-http")
                });
            }

            return new CheckOutcome(CheckCode, CheckStatus.Passed, Array.Empty<CheckFinding>());
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "HTTPS redirect probe başarısız: {Uri}", httpUri);
            return new CheckOutcome(CheckCode, CheckStatus.Skipped, Array.Empty<CheckFinding>(), ex.Message);
        }
    }
}
