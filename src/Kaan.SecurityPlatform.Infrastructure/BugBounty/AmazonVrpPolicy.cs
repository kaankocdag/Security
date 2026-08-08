using Kaan.SecurityPlatform.Application.Features.BugBounty;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Infrastructure.BugBounty;

/// <summary>
/// Amazon Vulnerability Research Program / HackerOne tarzı eligibility kuralları.
/// Eksik güvenlik başlığı tek başına gönderilmez.
/// </summary>
public sealed class AmazonVrpPolicy : IBugBountyProgramPolicy
{
    public const string PolicyKeyConstant = BugBountyProgramKeys.AmazonVrp;

    public string PolicyKey => PolicyKeyConstant;
    public string DisplayName => "Amazon VRP / HackerOne-style";

    public SubmissionRecommendation Evaluate(BugBountyPolicyCategory category, bool demonstratedImpact)
    {
        return category switch
        {
            BugBountyPolicyCategory.MissingSecurityHeaders => SubmissionRecommendation.DoNotSubmit,
            BugBountyPolicyCategory.MissingCookieFlags => SubmissionRecommendation.DoNotSubmit,
            BugBountyPolicyCategory.Clickjacking =>
                demonstratedImpact ? SubmissionRecommendation.ManualReview : SubmissionRecommendation.DoNotSubmit,
            BugBountyPolicyCategory.ScannerOutputOnly => SubmissionRecommendation.DoNotSubmit,
            BugBountyPolicyCategory.InformationDisclosure => SubmissionRecommendation.ManualReview,
            BugBountyPolicyCategory.MisconfigurationWithDemonstratedImpact =>
                demonstratedImpact ? SubmissionRecommendation.ManualReview : SubmissionRecommendation.DoNotSubmit,
            BugBountyPolicyCategory.Xss =>
                demonstratedImpact ? SubmissionRecommendation.Submit : SubmissionRecommendation.ManualReview,
            BugBountyPolicyCategory.SqlInjection =>
                demonstratedImpact ? SubmissionRecommendation.Submit : SubmissionRecommendation.ManualReview,
            BugBountyPolicyCategory.Idor =>
                demonstratedImpact ? SubmissionRecommendation.Submit : SubmissionRecommendation.ManualReview,
            BugBountyPolicyCategory.AuthenticationBypass =>
                demonstratedImpact ? SubmissionRecommendation.Submit : SubmissionRecommendation.ManualReview,
            BugBountyPolicyCategory.PrivilegeEscalation =>
                demonstratedImpact ? SubmissionRecommendation.Submit : SubmissionRecommendation.ManualReview,
            _ => demonstratedImpact
                ? SubmissionRecommendation.ManualReview
                : SubmissionRecommendation.DoNotSubmit
        };
    }
}
