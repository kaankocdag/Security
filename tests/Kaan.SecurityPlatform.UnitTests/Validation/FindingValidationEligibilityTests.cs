using FluentAssertions;
using Kaan.SecurityPlatform.Application.Features.Validation;
using Kaan.SecurityPlatform.Domain.Entities.Validation;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.HackerOne;
using Kaan.SecurityPlatform.Infrastructure.Validation;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.Validation;

public sealed class FindingValidationEligibilityTests
{
    private readonly SubmissionEligibilityEvaluator _eligibility = new();
    private readonly SensitiveSurfaceAnalyzer _surface = new();

    [Fact]
    public void Admin_login_page_is_not_confirmed_vulnerability()
    {
        var analysis = _surface.Analyze(
            "https://example.com/admin",
            200,
            "https://example.com/login",
            ["https://example.com/admin → 302 → https://example.com/login"],
            "text/html",
            """<html><title>Login</title><body><input type="password" name="password"/>Sign in</body></html>""");

        analysis.ConfirmedVulnerability.Should().BeFalse();
        analysis.DemonstratedImpact.Should().BeFalse();
        analysis.SubmissionRecommendation.Should().Be(SubmissionRecommendation.DoNotSubmit);
    }

    [Fact]
    public void Admin_403_is_secure_behavior_not_vulnerability()
    {
        var analysis = _surface.Analyze(
            "https://example.com/admin",
            403,
            "https://example.com/admin",
            [],
            "text/html",
            "<html><title>Forbidden</title><body>Access denied</body></html>");

        analysis.AccessDeniedDetected.Should().BeTrue();
        analysis.ConfirmedVulnerability.Should().BeFalse();
        analysis.SubmissionRecommendation.Should().Be(SubmissionRecommendation.DoNotSubmit);
    }

    [Fact]
    public void Same_public_content_is_not_access_control_impact()
    {
        var policy = new PolicyDecision(true, true, true, true, null, null);
        var exec = new ValidatorExecutionResult(
            ValidationStatus.CandidateOnly,
            false,
            false,
            ValidationImpactType.None,
            ValidationConfidence.Medium,
            "expected",
            "Both sessions received equivalent content; same public content is not access-control impact.",
            ["Same public content is not demonstrated access-control impact."],
            Array.Empty<ValidationEvidence>(),
            ReproductionCount: 1);

        var decision = _eligibility.Evaluate(
            new ImpactAssessment(false, false, ValidationImpactType.None, ValidationConfidence.Medium),
            policy,
            exec);

        decision.SubmissionEligible.Should().BeFalse();
        decision.PotentialRewardEligible.Should().BeFalse();
        decision.Recommendation.Should().NotBe(ValidationSubmissionRecommendation.SubmitCandidate);
    }

    [Fact]
    public void DemonstratedImpact_false_keeps_Submission_and_Reward_false()
    {
        var policy = new PolicyDecision(true, true, true, true, null, null);
        var exec = new ValidatorExecutionResult(
            ValidationStatus.CandidateOnly,
            false,
            false,
            ValidationImpactType.None,
            ValidationConfidence.High,
            "expected",
            "Administrative path reachability alone does not demonstrate an access-control vulnerability.",
            ["Administrative path reachability alone does not demonstrate an access-control vulnerability."],
            Array.Empty<ValidationEvidence>(),
            ReproductionCount: 1);

        var decision = _eligibility.Evaluate(
            new ImpactAssessment(false, false, ValidationImpactType.None, ValidationConfidence.High),
            policy,
            exec);

        decision.SubmissionEligible.Should().BeFalse();
        decision.PotentialRewardEligible.Should().BeFalse();
        decision.EligibilityReason.Should().Contain("Administrative path reachability");
    }

    [Fact]
    public void Scope_out_blocks_validation_eligibility()
    {
        var policy = new PolicyDecision(false, false, false, false, "Target is out of scope.", ValidationStatus.BlockedByPolicy);
        var exec = new ValidatorExecutionResult(
            ValidationStatus.BlockedByPolicy, false, false, ValidationImpactType.None,
            ValidationConfidence.Low, "", "blocked", [], Array.Empty<ValidationEvidence>());

        var decision = _eligibility.Evaluate(
            new ImpactAssessment(true, true, ValidationImpactType.UnauthorizedDataRead, ValidationConfidence.High),
            policy,
            exec);

        decision.SubmissionEligible.Should().BeFalse();
        decision.PotentialRewardEligible.Should().BeFalse();
        decision.Recommendation.Should().Be(ValidationSubmissionRecommendation.DoNotSubmit);
    }

    [Fact]
    public void EvidenceRedactor_masks_secrets_and_emails()
    {
        var redactor = new EvidenceRedactor();
        var redacted = redactor.RedactBody("Authorization: Bearer abc.def.ghi contact=user@example.com");
        redacted.Should().Contain("[redacted]");
        redacted.Should().NotContain("Bearer abc");
        redacted.Should().NotContain("user@example.com");
    }

    [Fact]
    public void SubmitCandidate_requires_confirmed_impact_and_evidence()
    {
        var policy = new PolicyDecision(true, true, true, true, null, null);
        var evidence = new ValidationEvidence
        {
            RequestMethod = "GET",
            RedactedRequestUrl = "https://example.com/resource",
            ResponseStatusCode = 200,
            SessionRole = ValidationSessionRole.TestAccountB
        };
        var exec = new ValidatorExecutionResult(
            ValidationStatus.Confirmed,
            true,
            true,
            ValidationImpactType.UnauthorizedDataRead,
            ValidationConfidence.High,
            "owner only",
            "B read A-owned resource",
            [],
            [evidence],
            ReproductionCount: 2);

        var decision = _eligibility.Evaluate(
            new ImpactAssessment(true, true, ValidationImpactType.UnauthorizedDataRead, ValidationConfidence.High),
            policy,
            exec);

        decision.Recommendation.Should().Be(ValidationSubmissionRecommendation.SubmitCandidate);
        decision.SubmissionEligible.Should().BeTrue();
        decision.PotentialRewardEligible.Should().BeTrue();
        decision.EligibilityReason.Should().Contain("Reward not guaranteed");
    }
}
