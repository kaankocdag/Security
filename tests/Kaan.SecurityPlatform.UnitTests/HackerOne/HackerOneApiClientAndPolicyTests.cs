using FluentAssertions;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.BugBounty;
using Kaan.SecurityPlatform.Infrastructure.HackerOne;
using Microsoft.Extensions.Options;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.HackerOne;

public sealed class HackerOneApiClientAndPolicyTests
{
    [Fact]
    public async Task Null_client_rejects_when_api_disabled()
    {
        var client = new NullHackerOneApiClient(Options.Create(new HackerOneOptions { ApiEnabled = false }));
        client.IsEnabled.Should().BeFalse();

        var ct = TestContext.Current.CancellationToken;
        var list = await client.ListProgramsAsync(ct);
        list.IsFailure.Should().BeTrue();
        list.ErrorCode.Should().Be("hackerone_api_disabled");

        var submit = await client.SubmitReportAsync(new HackerOneSubmitPayload("amazonvrp", "t", "High", "body"), ct);
        submit.IsFailure.Should().BeTrue();
        submit.ErrorCode.Should().Be("hackerone_api_disabled");
    }

    [Fact]
    public void Privilege_escalation_without_demonstrated_impact_is_manual_review()
    {
        var policy = new AmazonVrpPolicy();
        policy.Evaluate(BugBountyPolicyCategory.PrivilegeEscalation, demonstratedImpact: false)
            .Should().Be(SubmissionRecommendation.ManualReview);
        policy.Evaluate(BugBountyPolicyCategory.PrivilegeEscalation, demonstratedImpact: true)
            .Should().Be(SubmissionRecommendation.Submit);
    }

    [Fact]
    public void Asc_xss_fingerprint_is_candidate_never_auto_submit()
    {
        var sut = new FindingValidationClassifier([new AmazonVrpPolicy()]);
        var finding = new Finding
        {
            Title = "Reflected marker",
            Description = "candidate",
            Severity = Severity.Medium,
            CheckCode = "asc.xss-reflection",
            Fingerprint = "asc.xss.reflected-marker",
            ReflectionContext = ReflectionContext.HtmlText,
            HtmlEncoded = false,
            AttributeEncoded = false
        };

        sut.Classify(finding);

        finding.FindingClass.Should().Be(FindingClass.VulnerabilityCandidate);
        finding.BugBountySeverity.Should().Be(BugBountySeverity.Unassigned);
        finding.SubmissionRecommendation.Should().Be(SubmissionRecommendation.ManualReview);
        finding.BugBountyEligible.Should().BeFalse();
        finding.RequiresManualValidation.Should().BeTrue();
        finding.PolicyCategory.Should().Be(BugBountyPolicyCategory.Xss);
    }
}
