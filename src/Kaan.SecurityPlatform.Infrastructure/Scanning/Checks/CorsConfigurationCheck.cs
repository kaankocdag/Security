using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Checks;

public sealed class CorsConfigurationCheck : IPassiveSecurityCheck
{
    private readonly SecureHttpClientFactory _httpFactory;
    private readonly ILogger<CorsConfigurationCheck> _logger;

    public CorsConfigurationCheck(SecureHttpClientFactory httpFactory, ILogger<CorsConfigurationCheck> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public string CheckCode => "http.cors-configuration";
    public string DisplayName => "CORS Konfigürasyonu";
    public string Category => "CORS";
    public ScanType SupportedScanTypes => ScanType.FullPassive | ScanType.PassiveWeb;
    public int Order => 40;

    public async Task<CheckOutcome> RunAsync(ScanContext context, CancellationToken cancellationToken = default)
    {
        var findings = new List<CheckFinding>();
        using var client = _httpFactory.Create(timeout: TimeSpan.FromSeconds(10));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, context.TargetUri);
            request.Headers.Add("Origin", "https://kaan-security-cors-probe.example");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.Headers.TryGetValues("Access-Control-Allow-Origin", out var originValues))
            {
                return new CheckOutcome(CheckCode, CheckStatus.Passed, findings, "CORS başlığı yansıtılmadı.");
            }

            var origin = string.Join(", ", originValues);
            var allowCredentials = response.Headers.TryGetValues("Access-Control-Allow-Credentials", out var credValues)
                && string.Join(", ", credValues).Contains("true", StringComparison.OrdinalIgnoreCase);

            if (origin == "*" && allowCredentials)
            {
                findings.Add(new CheckFinding(
                    Title: "CORS geniş yansıma ile birlikte credential izni",
                    Description: "Access-Control-Allow-Origin '*' değeri ile birlikte Access-Control-Allow-Credentials true olarak dönüyor. Bu kombinasyon tarayıcı standardında yasaklı olmasına rağmen ayarları riskli tutar.",
                    Severity: Severity.High,
                    Confidence: ConfidenceLevel.Confirmed,
                    Category: "CORS",
                    CweCode: "CWE-942",
                    AffectedUrl: context.TargetUri.ToString(),
                    Evidence: $"Origin: {origin}; Credentials: true",
                    Remediation: "Origin'ler için beyaz liste kullanın. 'Access-Control-Allow-Credentials' aktifken '*' kullanmayın.",
                    Fingerprint: "cors.wildcard-with-credentials"));
            }
            else if (origin.Contains("kaan-security-cors-probe.example", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new CheckFinding(
                    Title: "CORS herhangi bir origini yansıtıyor",
                    Description: "Kontrol amaçlı gönderilen sahte origin sunucudan olduğu gibi geri döndü. Bu, uygulamanın gelen origin'i doğrulamadan yansıttığı anlamına gelir.",
                    Severity: Severity.High,
                    Confidence: allowCredentials ? ConfidenceLevel.Confirmed : ConfidenceLevel.StrongIndication,
                    Category: "CORS",
                    CweCode: "CWE-942",
                    AffectedUrl: context.TargetUri.ToString(),
                    Evidence: $"Origin: {origin}",
                    Remediation: "Origin doğrulaması için beyaz liste (allowlist) kullanın; header'ı yansıtmayın.",
                    Fingerprint: "cors.reflected-origin"));
            }
            else if (origin == "*")
            {
                findings.Add(new CheckFinding(
                    Title: "CORS her origin'e açık",
                    Description: "Access-Control-Allow-Origin '*' değerinde. Herhangi bir origin bu API'ye erişebilir.",
                    Severity: Severity.Low,
                    Confidence: ConfidenceLevel.Confirmed,
                    Category: "CORS",
                    AffectedUrl: context.TargetUri.ToString(),
                    Evidence: origin,
                    Remediation: "Halka açık olması gerekmiyorsa origin allowlist tanımlayın.",
                    Fingerprint: "cors.wildcard"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "CORS kontrolü sırasında hata");
            return new CheckOutcome(CheckCode, CheckStatus.Skipped, findings, ex.Message);
        }

        return new CheckOutcome(CheckCode, findings.Count == 0 ? CheckStatus.Passed : CheckStatus.IssuesFound, findings);
    }
}
