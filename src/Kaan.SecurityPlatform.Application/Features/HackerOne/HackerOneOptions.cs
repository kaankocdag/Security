namespace Kaan.SecurityPlatform.Application.Features.HackerOne;

public sealed class HackerOneOptions
{
    public const string SectionName = "HackerOne";

    /// <summary>Varsayılan false — API sync/submit kapalı; Copy/Open çalışır.</summary>
    public bool ApiEnabled { get; set; }

    public string BaseUrl { get; set; } = "https://api.hackerone.com/v1";
    public string OpenReportUrlTemplate { get; set; } = "https://hackerone.com/{handle}";
    public int MinReadinessScoreForSubmit { get; set; } = 70;

    public AmazonVrpScanOptions AmazonVrp { get; set; } = new();
}

public sealed class AmazonVrpScanOptions
{
    public string UserAgent { get; set; } = "KaanSecurityPlatform-AmazonVRP-Candidate/1.0 (+https://local.dev; research)";
    public int RateLimitPerMinute { get; set; } = 20;
}
