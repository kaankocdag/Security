using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Domain.Entities.Findings;

namespace Kaan.SecurityPlatform.Application.Features.BugBounty;

public interface IBugBountyProgramPolicy
{
    string PolicyKey { get; }
    string DisplayName { get; }
    SubmissionRecommendation Evaluate(BugBountyPolicyCategory category, bool demonstratedImpact);
}

public interface IFindingValidationClassifier
{
    /// <summary>
    /// Scanner çıktısını sınıflandırır; TechnicalSeverity / eligibility alanlarını doldurur.
    /// </summary>
    void Classify(Finding finding, string programPolicyKey = BugBountyProgramKeys.AmazonVrp);
}

public static class BugBountyProgramKeys
{
    public const string AmazonVrp = "AmazonVRP";
}
