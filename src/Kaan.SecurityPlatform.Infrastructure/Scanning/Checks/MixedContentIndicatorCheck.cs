using System.Text.RegularExpressions;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Checks;

public sealed class MixedContentIndicatorCheck : IPassiveSecurityCheck
{
    private static readonly Regex ResourcePattern = new(
        "(?<attr>src|href)\\s*=\\s*[\"'](?<url>http://[^\"'\\s>]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly SecureHttpClientFactory _httpFactory;
    private readonly ILogger<MixedContentIndicatorCheck> _logger;

    public MixedContentIndicatorCheck(SecureHttpClientFactory httpFactory, ILogger<MixedContentIndicatorCheck> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public string CheckCode => "http.mixed-content";
    public string DisplayName => "Mixed Content";
    public string Category => "Transport Security";
    public ScanType SupportedScanTypes => ScanType.FullPassive;
    public int Order => 50;

    public async Task<CheckOutcome> RunAsync(ScanContext context, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(context.TargetUri.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            return new CheckOutcome(CheckCode, CheckStatus.Skipped, Array.Empty<CheckFinding>(), "HTTPS olmayan hedefte mixed-content anlamsız.");
        }

        try
        {
            using var client = _httpFactory.Create(timeout: TimeSpan.FromSeconds(12));
            using var response = await client.GetAsync(context.TargetUri, HttpCompletionOption.ResponseContentRead, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (body.Length > 512 * 1024)
            {
                body = body[..(512 * 1024)];
            }

            var matches = ResourcePattern.Matches(body);
            if (matches.Count == 0)
            {
                return new CheckOutcome(CheckCode, CheckStatus.Passed, Array.Empty<CheckFinding>());
            }

            var samples = matches
                .Cast<Match>()
                .Select(m => m.Groups["url"].Value)
                .Where(url => !url.Contains("://localhost", StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .Take(5)
                .ToArray();

            if (samples.Length == 0)
            {
                return new CheckOutcome(CheckCode, CheckStatus.Passed, Array.Empty<CheckFinding>());
            }

            return new CheckOutcome(CheckCode, CheckStatus.IssuesFound, new[]
            {
                new CheckFinding(
                    Title: "Mixed content göstergesi",
                    Description: $"HTTPS sayfada HTTP kaynaklara referans veren en az {matches.Count} unsur tespit edildi. Modern tarayıcılar bu tür içerikleri engelleyebilir veya sertifika güveninin kırılmasına neden olur.",
                    Severity: Severity.Medium,
                    Confidence: ConfidenceLevel.StrongIndication,
                    Category: Category,
                    CweCode: "CWE-311",
                    AffectedUrl: context.TargetUri.ToString(),
                    Evidence: string.Join("\n", samples),
                    Remediation: "Tüm kaynak referanslarını 'https://' veya protokolsüz '//' olarak güncelleyin. CSP 'upgrade-insecure-requests' direktifi ile güvenceye alın.",
                    Fingerprint: "http.mixed-content")
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Mixed content check başarısız: {Uri}", context.TargetUri);
            return new CheckOutcome(CheckCode, CheckStatus.Skipped, Array.Empty<CheckFinding>(), ex.Message);
        }
    }
}
