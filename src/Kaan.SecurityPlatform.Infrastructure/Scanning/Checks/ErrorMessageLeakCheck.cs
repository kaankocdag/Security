using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Checks;

public sealed class ErrorMessageLeakCheck : IPassiveSecurityCheck
{
    private static readonly (string Signal, string Family)[] Signals = new (string, string)[]
    {
        ("System.Data.SqlClient.SqlException", ".NET SQL Exception"),
        ("at System.Web", ".NET stack trace"),
        ("Microsoft OLE DB Provider", "OLE DB error"),
        ("java.lang.NullPointerException", "Java stack trace"),
        ("at org.springframework", "Spring stack trace"),
        ("Traceback (most recent call last)", "Python traceback"),
        ("PHP Fatal error", "PHP fatal error"),
        ("PHP Warning", "PHP warning"),
        ("Uncaught Error:", "PHP uncaught error"),
        ("ORA-01756", "Oracle error"),
        ("psql:", "PostgreSQL error"),
        ("You have an error in your SQL syntax", "MySQL error"),
        ("Warning: mysql_", "MySQL warning")
    };

    private readonly SecureHttpClientFactory _httpFactory;
    private readonly ILogger<ErrorMessageLeakCheck> _logger;

    public ErrorMessageLeakCheck(SecureHttpClientFactory httpFactory, ILogger<ErrorMessageLeakCheck> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public string CheckCode => "http.error-leak";
    public string DisplayName => "Hata Mesajı Sızıntısı";
    public string Category => "Information Disclosure";
    public ScanType SupportedScanTypes => ScanType.FullPassive | ScanType.InformationDisclosure;
    public int Order => 70;

    public async Task<CheckOutcome> RunAsync(ScanContext context, CancellationToken cancellationToken = default)
    {
        var probeUri = new Uri(context.TargetUri, "/kaan-security-nonexistent-" + Guid.NewGuid().ToString("N")[..12]);
        try
        {
            using var client = _httpFactory.Create(timeout: TimeSpan.FromSeconds(8), allowRedirects: false);
            using var response = await client.GetAsync(probeUri, HttpCompletionOption.ResponseContentRead, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (body.Length > 128 * 1024)
            {
                body = body[..(128 * 1024)];
            }

            var hit = Signals.FirstOrDefault(s => body.Contains(s.Signal, StringComparison.OrdinalIgnoreCase));
            if (hit.Signal is null)
            {
                return new CheckOutcome(CheckCode, CheckStatus.Passed, Array.Empty<CheckFinding>());
            }

            return new CheckOutcome(CheckCode, CheckStatus.IssuesFound, new[]
            {
                new CheckFinding(
                    Title: "Sunucu hata mesajı sızıntısı",
                    Description: $"Hedefe olmayan bir yol istendiğinde detaylı sunucu hata çıktısı ({hit.Family}) döndü. Bu bilgiler saldırganlara teknoloji ve yapı hakkında ipucu verir.",
                    Severity: Severity.Medium,
                    Confidence: ConfidenceLevel.StrongIndication,
                    Category: Category,
                    CweCode: "CWE-209",
                    OwaspCategory: "A05:2021 - Security Misconfiguration",
                    AffectedUrl: probeUri.ToString(),
                    Evidence: hit.Signal,
                    Remediation: "Üretim ortamında ayrıntılı hata çıktısını kapatın. Genel 404/500 sayfaları gösterin ve stack trace loglayın.",
                    Fingerprint: "info.error-leak")
            });
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error leak probe başarısız: {Uri}", probeUri);
            return new CheckOutcome(CheckCode, CheckStatus.Skipped, Array.Empty<CheckFinding>(), ex.Message);
        }
    }
}
