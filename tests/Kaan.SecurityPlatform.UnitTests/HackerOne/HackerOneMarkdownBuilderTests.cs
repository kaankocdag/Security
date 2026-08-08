using FluentAssertions;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Kaan.SecurityPlatform.Infrastructure.HackerOne;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.HackerOne;

public sealed class HackerOneMarkdownBuilderTests
{
    private readonly HackerOneMarkdownBuilder _sut = new();

    [Fact]
    public void Build_is_english_en_US_and_matches_candidate_report_shape()
    {
        var md = _sut.Build(new HackerOneReportDraftFields(
            Title: "Reflected Input / XSS Candidate",
            Severity: "Unassigned",
            Asset: "example.com",
            Weakness: "Potential Weakness: CWE-79",
            Impact:
                "A unique harmless marker supplied through a query parameter was reflected in the HTTP response body.\n\n" +
                "This confirms input reflection only. No executable JavaScript or browser-side code execution has been demonstrated.",
            StepsToReproduce: "1. Open `https://example.com/search?[redacted]`\n2. Observe",
            ProofOfConcept: "Marker reflected",
            Notes: "Manual validation required",
            ConfirmedVulnerability: false,
            DemonstratedImpact: false,
            BugBountySeverityLabel: "Unassigned",
            Language: HackerOneReportLanguage.Code,
            FindingType: "XSS Candidate",
            CandidateSeverity: "Medium",
            ExploitabilityLabel: "Requires Manual Validation",
            SubmissionRecommendationLabel: "Manual Review",
            Summary: "Reflection candidate only.",
            EligibilityReason: "Not bug bounty eligible without demonstrated impact."));

        md.Should().Contain("**Language:** en-US");
        md.Should().Contain("**Finding Type:** XSS Candidate");
        md.Should().Contain("**Candidate Severity:** Medium");
        md.Should().Contain("**Confirmed Vulnerability:** No");
        md.Should().Contain("**Demonstrated Impact:** No");
        md.Should().Contain("**Submission Recommendation:** Manual Review");
        md.Should().Contain("Potential Weakness: CWE-79");
        md.Should().Contain("## Steps to Reproduce");
        md.Should().NotContain("Şiddet");
        md.Should().NotContain("BB adayı");
        md.Should().NotContain("TeknikŞiddet");
        md.Should().NotContain("Öneri");
    }

    [Fact]
    public void FormatSafeUrl_wraps_redacted_urls_as_code_not_markdown_links()
    {
        var safe = _sut.FormatSafeUrlForSteps("https://example.com/path?[redacted]");
        safe.Should().StartWith("`");
        safe.Should().EndWith("`");

        var md = _sut.Build(new HackerOneReportDraftFields(
            "t", "Unassigned", "a", "Potential Weakness: CWE-79",
            "impact text long enough here for readiness scoring purposes",
            "1. Navigate to [broken](https://example.com/?[redacted])\n2. Next step with details here",
            "poc evidence long enough for readiness",
            null,
            false,
            false,
            "Unassigned",
            HackerOneReportLanguage.Code));

        md.Should().NotContain("](https://example.com/?[redacted])");
        md.Should().Contain("`https://example.com/?[redacted]`");
    }

    [Fact]
    public void Readiness_penalizes_empty_and_short_fields()
    {
        var empty = _sut.ComputeReadinessScore(new HackerOneReportDraftFields("", "", "", "", "", "", "", null));
        empty.Should().BeLessThan(40);
    }
}
