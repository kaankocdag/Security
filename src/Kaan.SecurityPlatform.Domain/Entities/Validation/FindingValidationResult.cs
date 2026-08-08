using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Validation;

public class FindingValidationResult : BaseEntity
{
    public Guid ValidationRunId { get; set; }
    public bool ConfirmedVulnerability { get; set; }
    public bool DemonstratedImpact { get; set; }
    public ValidationImpactType ImpactType { get; set; } = ValidationImpactType.None;
    public ValidationConfidence Confidence { get; set; } = ValidationConfidence.Low;
    public ValidationSubmissionRecommendation SubmissionRecommendation { get; set; } =
        ValidationSubmissionRecommendation.DoNotSubmit;
    public bool SubmissionEligible { get; set; }
    public bool PotentialRewardEligible { get; set; }
    public string? EligibilityReason { get; set; }
    public string? ManualReviewReasons { get; set; }
    public string? ExpectedResult { get; set; }
    public string? ActualResult { get; set; }
    public string ValidatorVersion { get; set; } = "1.0.0";
    public int ReproductionCount { get; set; }
    public string? TestAccountRolesUsed { get; set; }

    public FindingValidationRun? ValidationRun { get; set; }
}
