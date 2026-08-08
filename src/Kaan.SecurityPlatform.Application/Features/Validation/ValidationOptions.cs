namespace Kaan.SecurityPlatform.Application.Features.Validation;

public sealed class ValidationOptions
{
    public const string SectionName = "FindingValidation";

    public int DefaultMaxRequestCount { get; set; } = 10;
    public int DelayBetweenRequestsMs { get; set; } = 350;
    public int MaxConcurrencyPerTarget { get; set; } = 1;
    public string UserAgent { get; set; } =
        "Kaan.SecurityPlatform-FindingValidation/1.0 (+safe-research; read-only)";

    /// <summary>Development only — never enable in production.</summary>
    public bool AllowMockAuthorization { get; set; }
}
