using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Checks;

public sealed class HttpsCheck : IPassiveSecurityCheck
{
    private readonly SecureHttpClientFactory _httpFactory;
    private readonly ILogger<HttpsCheck> _logger;

    public HttpsCheck(SecureHttpClientFactory httpFactory, ILogger<HttpsCheck> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public string CheckCode => "https.usage";
    public string DisplayName => "HTTPS Kullanımı";
    public string Category => "Transport Security";
    public ScanType SupportedScanTypes => ScanType.FullPassive | ScanType.PassiveWeb;
    public int Order => 10;

    public async Task<CheckOutcome> RunAsync(ScanContext context, CancellationToken cancellationToken = default)
    {
        var findings = new List<CheckFinding>();

        using var client = _httpFactory.Create(timeout: TimeSpan.FromSeconds(12));
        var httpUrl = new UriBuilder(context.TargetUri) { Scheme = "http", Port = -1 }.Uri;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, httpUrl);
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var finalUri = response.RequestMessage?.RequestUri;
            var redirectedToHttps = finalUri?.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) == true;

            if (!redirectedToHttps)
            {
                findings.Add(new CheckFinding(
                    Title: "HTTP trafiği HTTPS'e yönlendirilmiyor",
                    Description: "HTTP isteği HTTPS adresine yönlendirilmedi. Bu, veri şifrelemesi olmadan tarayıcı ile iletişim kurulmasına neden olur.",
                    Severity: Severity.High,
                    Confidence: ConfidenceLevel.Confirmed,
                    Category: "Transport Security",
                    CweCode: "CWE-319",
                    OwaspCategory: "A02:2021 - Cryptographic Failures",
                    AffectedUrl: httpUrl.ToString(),
                    Remediation: "Web sunucusunda tüm HTTP isteklerini 301 kalıcı yönlendirme ile HTTPS'e taşıyın.",
                    RemediationExampleConfig: "Nginx: return 301 https://$host$request_uri;",
                    TurkishExecutiveSummary: "Sitenize HTTP üzerinden gelen istekler güvenli HTTPS bağlantısına yönlendirilmiyor. Kullanıcı verileri şifresiz iletildiği için kritik risk oluşturur.",
                    BusinessImpact: "Kullanıcı kimlik bilgileri, oturum çerezleri ve kişisel veriler ağ üzerinde açıkta iletilir.",
                    Fingerprint: "https.no-redirect"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "HTTPS yönlendirme kontrolü sırasında istek başarısız oldu: {Url}", httpUrl);
        }

        var status = findings.Count == 0 ? CheckStatus.Passed : CheckStatus.IssuesFound;
        return new CheckOutcome(CheckCode, status, findings);
    }
}
