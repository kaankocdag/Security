using FluentAssertions;
using Kaan.SecurityPlatform.Domain.Enums;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.HackerOne;

/// <summary>
/// Submit kapılarının saf mantık doğrulaması (workspace servisi DB gerektirir).
/// </summary>
public sealed class SubmitGateLogicTests
{
    [Theory]
    [InlineData(false, true, 90, SubmissionRecommendation.Submit, true, false, "api_disabled")]
    [InlineData(true, false, 90, SubmissionRecommendation.Submit, true, false, "no_explicit_confirm")]
    [InlineData(true, true, 40, SubmissionRecommendation.Submit, true, false, "readiness_low")]
    [InlineData(true, true, 90, SubmissionRecommendation.DoNotSubmit, true, false, "do_not_submit")]
    [InlineData(true, true, 90, SubmissionRecommendation.ManualReview, false, true, "manual_review_ok")]
    [InlineData(true, true, 90, SubmissionRecommendation.Submit, true, true, "eligible_ok")]
    public void Gates_match_plan_rules(
        bool apiEnabled,
        bool explicitConfirm,
        int readiness,
        SubmissionRecommendation recommendation,
        bool bugBountyEligible,
        bool expectAllow,
        string _)
    {
        const int minReadiness = 70;
        var allowed =
            apiEnabled
            && explicitConfirm
            && readiness >= minReadiness
            && recommendation != SubmissionRecommendation.DoNotSubmit
            && (bugBountyEligible || recommendation == SubmissionRecommendation.ManualReview);

        allowed.Should().Be(expectAllow);
    }
}
