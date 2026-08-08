using FluentAssertions;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.BugBounty;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.HackerOne;

public sealed class XssCandidateClassificationTests
{
    private readonly FindingValidationClassifier _sut = new([new AmazonVrpPolicy()]);

    [Fact]
    public void Reflected_marker_is_vulnerability_candidate_not_vulnerability()
    {
        var finding = BaseXssFinding();
        _sut.Classify(finding);

        finding.FindingClass.Should().Be(FindingClass.VulnerabilityCandidate);
        finding.BugBountySeverity.Should().Be(BugBountySeverity.Unassigned);
        finding.TechnicalPotentialSeverity.Should().Be(Severity.Medium);
        finding.DemonstratedImpact.Should().BeFalse();
        finding.SubmissionRecommendation.Should().NotBe(SubmissionRecommendation.Submit);
        finding.BugBountyEligible.Should().BeFalse();
    }

    [Fact]
    public void Properly_encoded_reflection_is_do_not_submit()
    {
        var finding = BaseXssFinding();
        finding.HtmlEncoded = true;
        finding.ReflectionContext = ReflectionContext.HtmlText;

        _sut.Classify(finding);

        finding.SubmissionRecommendation.Should().Be(SubmissionRecommendation.DoNotSubmit);
        finding.EligibilityReason.Should().Be("Properly encoded reflected input; no XSS impact.");
    }

    [Fact]
    public void Unknown_context_is_manual_review()
    {
        var finding = BaseXssFinding();
        finding.HtmlEncoded = false;
        finding.AttributeEncoded = false;
        finding.ReflectionContext = ReflectionContext.Unknown;

        _sut.Classify(finding);

        finding.SubmissionRecommendation.Should().Be(SubmissionRecommendation.ManualReview);
        finding.EligibilityReason.Should().Contain("Reflection context unclear");
        finding.EligibilityReason.Should().Contain("Manual Review");
        finding.EligibilityReason.Should().NotContain("BB adayı");
        finding.EligibilityReason.Should().NotContain("TeknikŞiddet");
    }

    private static Finding BaseXssFinding() => new()
    {
        Title = "Reflected Input / XSS Candidate",
        Description = "candidate",
        Severity = Severity.Medium,
        CheckCode = "asc.xss-reflection",
        Fingerprint = "asc.xss.reflected-marker",
        CweCode = "CWE-79"
    };
}
