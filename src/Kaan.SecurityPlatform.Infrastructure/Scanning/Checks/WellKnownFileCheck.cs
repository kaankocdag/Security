using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Checks;

public sealed class WellKnownFileCheck : IPassiveSecurityCheck
{
    private readonly SecureHttpClientFactory _httpFactory;
    private readonly ILogger<WellKnownFileCheck> _logger;

    public WellKnownFileCheck(SecureHttpClientFactory httpFactory, ILogger<WellKnownFileCheck> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public string CheckCode => "http.well-known";
    public string DisplayName => "security.txt / robots.txt / sitemap.xml";
    public string Category => "Discovery";
    public ScanType SupportedScanTypes => ScanType.FullPassive | ScanType.InformationDisclosure;
    public int Order => 90;

    public async Task<CheckOutcome> RunAsync(ScanContext context, CancellationToken cancellationToken = default)
    {
        var findings = new List<CheckFinding>();
        using var client = _httpFactory.Create(timeout: TimeSpan.FromSeconds(8));

        var baseUri = new UriBuilder(context.TargetUri) { Path = "/" }.Uri;

        await ProbeAsync(client, new Uri(baseUri, "/.well-known/security.txt"), cancellationToken, exists =>
        {
            if (!exists)
            {
                findings.Add(new CheckFinding(
                    Title: "security.txt eksik",
                    Description: "'/.well-known/security.txt' bulunamadı. Güvenlik araştırmacılarının size ulaşabilmesi için önerilir.",
                    Severity: Severity.Informational,
                    Confidence: ConfidenceLevel.Confirmed,
                    Category: "Discovery",
                    AffectedUrl: new Uri(baseUri, "/.well-known/security.txt").ToString(),
                    Remediation: "'/.well-known/security.txt' dosyasını RFC 9116 formatında yayınlayın.",
                    RemediationExampleConfig: "Contact: mailto:security@example.com\nExpires: 2027-01-01T00:00:00Z\nPreferred-Languages: tr,en",
                    Fingerprint: "wellknown.security-txt.missing"));
            }
        });

        await ProbeAsync(client, new Uri(baseUri, "/robots.txt"), cancellationToken, exists =>
        {
            if (!exists)
            {
                findings.Add(new CheckFinding(
                    Title: "robots.txt bulunamadı",
                    Description: "'robots.txt' bulunamadı. Bu dosya varsayılan davranışı belirtir ve tarama botlarına yönerge sağlar.",
                    Severity: Severity.Informational,
                    Confidence: ConfidenceLevel.Recommendation,
                    Category: "Discovery",
                    AffectedUrl: new Uri(baseUri, "/robots.txt").ToString(),
                    Remediation: "Kök dizinde robots.txt yayınlayın. Hassas dizinleri Disallow ile belirtin (bilgi ifşasına dikkat).",
                    Fingerprint: "wellknown.robots.missing"));
            }
        });

        await ProbeAsync(client, new Uri(baseUri, "/sitemap.xml"), cancellationToken, exists =>
        {
            if (!exists)
            {
                findings.Add(new CheckFinding(
                    Title: "sitemap.xml bulunamadı",
                    Description: "'sitemap.xml' bulunamadı. SEO ve tarama botları için yayınlanması önerilir.",
                    Severity: Severity.Informational,
                    Confidence: ConfidenceLevel.Recommendation,
                    Category: "Discovery",
                    AffectedUrl: new Uri(baseUri, "/sitemap.xml").ToString(),
                    Remediation: "Kök dizinde sitemap.xml oluşturun.",
                    Fingerprint: "wellknown.sitemap.missing"));
            }
        });

        return new CheckOutcome(CheckCode, CheckStatus.Passed, findings);
    }

    private async Task ProbeAsync(HttpClient client, Uri target, CancellationToken cancellationToken, Action<bool> callback)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, target);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            callback(response.IsSuccessStatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Probe başarısız: {Target}", target);
            callback(false);
        }
    }
}
