using FluentAssertions;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.BugBounty;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.BugBounty;

public sealed class FindingValidationClassifierTests
{
    private readonly FindingValidationClassifier _sut = new([new AmazonVrpPolicy()]);

    [Theory]
    [InlineData("sh.hsts.missing", FindingClass.HardeningRecommendation, false)]
    [InlineData("sh.csp.missing", FindingClass.HardeningRecommendation, false)]
    [InlineData("sh.nosniff.missing", FindingClass.HardeningRecommendation, false)]
    [InlineData("sh.permissions.missing", FindingClass.Informational, false)]
    [InlineData("sh.referrer.missing", FindingClass.Informational, false)]
    [InlineData("sh.clickjacking.missing", FindingClass.HardeningRecommendation, false)]
    [InlineData("wellknown.sitemap.missing", FindingClass.SeoIssue, false)]
    [InlineData("cookie.flags.session", FindingClass.SecurityMisconfiguration, false)]
    public void Header_and_seo_findings_are_not_bb_vulnerabilities(
        string fingerprint,
        FindingClass expectedClass,
        bool expectedEligible)
    {
        var finding = new Finding
        {
            Title = "test",
            Description = "test",
            Severity = Severity.High,
            CheckCode = "http.security-headers",
            Fingerprint = fingerprint
        };

        _sut.Classify(finding);

        finding.FindingClass.Should().Be(expectedClass);
        finding.DemonstratedImpact.Should().BeFalse();
        finding.BugBountyEligible.Should().Be(expectedEligible);
        finding.SubmissionRecommendation.Should().Be(SubmissionRecommendation.DoNotSubmit);
        ((int)finding.TechnicalSeverity).Should().BeLessThanOrEqualTo((int)Severity.Medium);
        finding.Severity.Should().Be(Severity.High); // scanner severity korunur
        finding.ProgramPolicyMatch.Should().Be(AmazonVrpPolicy.PolicyKeyConstant);
    }

    [Fact]
    public void High_scanner_severity_does_not_force_critical_technical_severity_for_hsts()
    {
        var finding = new Finding
        {
            Title = "HSTS",
            Description = "missing",
            Severity = Severity.Critical,
            CheckCode = "http.security-headers",
            Fingerprint = "sh.hsts.missing"
        };

        _sut.Classify(finding);

        finding.TechnicalSeverity.Should().Be(Severity.Low);
        finding.RequiresManualValidation.Should().BeTrue();
    }
}
