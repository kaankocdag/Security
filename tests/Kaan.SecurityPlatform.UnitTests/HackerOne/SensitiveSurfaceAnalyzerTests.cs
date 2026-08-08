using FluentAssertions;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Kaan.SecurityPlatform.Infrastructure.BugBounty;
using Kaan.SecurityPlatform.Infrastructure.HackerOne;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.HackerOne;

public sealed class SensitiveSurfaceAnalyzerTests
{
    private readonly SensitiveSurfaceAnalyzer _sut = new();
    private readonly FindingValidationClassifier _classifier = new([new AmazonVrpPolicy()]);
    private readonly HackerOneMarkdownBuilder _markdown = new();

    [Fact]
    public void Admin_exists_http_200_harmless_page_is_DoNotSubmit()
    {
        var result = _sut.Analyze(
            url: "https://example.com/admin",
            httpStatusCode: 200,
            finalUrl: "https://example.com/admin",
            redirectChain: [],
            contentType: "text/html",
            body: """
                  <html><head><title>Welcome</title></head>
                  <body><h1>Welcome to our site</h1><p>Learn more about our products.</p></body></html>
                  """);

        result.ManualReviewReasons.Should().BeEmpty();
        result.SubmissionRecommendation.Should().Be(SubmissionRecommendation.DoNotSubmit);
        result.FindingClass.Should().Be(FindingClass.Informational);
        result.FindingClass.Should().NotBe(FindingClass.Vulnerability);
        Classify(result).SubmissionRecommendation.Should().Be(SubmissionRecommendation.DoNotSubmit);
    }

    [Fact]
    public void Admin_login_redirect_is_DoNotSubmit()
    {
        var result = _sut.Analyze(
            url: "https://example.com/admin",
            httpStatusCode: 200,
            finalUrl: "https://example.com/login",
            redirectChain: ["https://example.com/admin → 302 → https://example.com/login"],
            contentType: "text/html",
            body: """
                  <html><head><title>Sign In</title></head>
                  <body><form><input type="password" name="password" /><button>Log in</button></form></body></html>
                  """);

        result.LoginPageDetected.Should().BeTrue();
        result.ManualReviewReasons.Should().BeEmpty();
        result.SubmissionRecommendation.Should().Be(SubmissionRecommendation.DoNotSubmit);
        Classify(result).FindingClass.Should().NotBe(FindingClass.Vulnerability);
    }

    [Fact]
    public void Admin_403_is_DoNotSubmit()
    {
        var result = _sut.Analyze(
            url: "https://example.com/admin",
            httpStatusCode: 403,
            finalUrl: "https://example.com/admin",
            redirectChain: [],
            contentType: "text/html",
            body: "<html><title>Forbidden</title><body>Access denied</body></html>");

        result.AccessDeniedDetected.Should().BeTrue();
        result.ManualReviewReasons.Should().BeEmpty();
        result.SubmissionRecommendation.Should().Be(SubmissionRecommendation.DoNotSubmit);
    }

    [Fact]
    public void Admin_privileged_ui_indicators_is_ManualReview_with_reasons()
    {
        var result = _sut.Analyze(
            url: "https://example.com/admin",
            httpStatusCode: 200,
            finalUrl: "https://example.com/admin",
            redirectChain: [],
            contentType: "text/html",
            body: """
                  <html><head><title>Admin Dashboard</title></head>
                  <body>
                    <h1>Administration Panel</h1>
                    <a href="/admin/users">User Management</a>
                    <button>Delete User</button>
                    <section>Role assignment</section>
                    <p>Internal console — manage roles and audit log</p>
                  </body></html>
                  """);

        result.PrivilegedFunctionalityDetected.Should().BeTrue();
        result.ManualReviewReasons.Should().NotBeEmpty();
        result.ManualReviewReasons.Should().Contain(r => r.Contains("administrative action", StringComparison.OrdinalIgnoreCase));
        result.SubmissionRecommendation.Should().Be(SubmissionRecommendation.ManualReview);
        result.FindingType.Should().Be("AccessControlCandidate");
        result.ConfirmedVulnerability.Should().BeFalse();

        var finding = Classify(result);
        finding.SubmissionRecommendation.Should().Be(SubmissionRecommendation.ManualReview);
        finding.FindingClass.Should().Be(FindingClass.VulnerabilityCandidate);
    }

    [Fact]
    public void Admin_unauthenticated_sensitive_data_indicators_is_high_priority_ManualReview()
    {
        var result = _sut.Analyze(
            url: "https://example.com/admin",
            httpStatusCode: 200,
            finalUrl: "https://example.com/admin",
            redirectChain: [],
            contentType: "text/html",
            body: """
                  <html><head><title>Accounts</title></head>
                  <body>
                    <p>customer pii export</p>
                    <ul>
                      <li>alice@corp.example</li>
                      <li>bob@corp.example</li>
                      <li>carol@corp.example</li>
                    </ul>
                    <code>api_key=sk_live_abcdefghijklmnopqrstuv</code>
                  </body></html>
                  """);

        result.SensitiveContentDetected.Should().BeTrue();
        result.SensitiveIdentifiersDetected.Should().BeTrue();
        result.HighPriorityManualReview.Should().BeTrue();
        result.Fingerprint.Should().Be("asc.access.surface-manualreview-high");
        result.ManualReviewReasons.Should().Contain(r => r.Contains("[high priority]", StringComparison.OrdinalIgnoreCase));
        result.SubmissionRecommendation.Should().Be(SubmissionRecommendation.ManualReview);
        result.ConfirmedVulnerability.Should().BeFalse();
        result.FindingClass.Should().NotBe(FindingClass.Vulnerability);

        var finding = Classify(result);
        finding.TechnicalPotentialSeverity.Should().Be(Severity.High);
        finding.SubmissionRecommendation.Should().Be(SubmissionRecommendation.ManualReview);
    }

    [Fact]
    public void Verified_unauthorized_privileged_access_is_Confirmed_Vulnerability()
    {
        var observation = _sut.Analyze(
            url: "https://example.com/admin",
            httpStatusCode: 200,
            finalUrl: "https://example.com/admin",
            redirectChain: [],
            contentType: "text/html",
            body: """
                  <html><head><title>Admin Dashboard</title></head>
                  <body>
                    <h1>Administration Panel</h1>
                    <a>User Management</a><button>Delete User</button>
                    <section>Role assignment</section>
                  </body></html>
                  """);

        var confirmed = _sut.MarkVerifiedUnauthorizedPrivilegedAccess(
            observation,
            "Low-privilege test account A retrieved admin user-management actions that account B (admin) can also reach; object-level check confirmed.");

        confirmed.ConfirmedVulnerability.Should().BeTrue();
        confirmed.DemonstratedImpact.Should().BeTrue();
        confirmed.UnauthorizedPrivilegedAccess.Should().BeTrue();
        confirmed.FindingClass.Should().Be(FindingClass.Vulnerability);
        confirmed.FindingType.Should().Be("Broken Access Control");
        confirmed.SubmissionRecommendation.Should().Be(SubmissionRecommendation.Submit);
        confirmed.Fingerprint.Should().Be("asc.access.confirmed-unauthorized");

        var finding = Classify(confirmed);
        finding.FindingClass.Should().Be(FindingClass.Vulnerability);
        finding.DemonstratedImpact.Should().BeTrue();
        finding.SubmissionRecommendation.Should().Be(SubmissionRecommendation.Submit);
        finding.BugBountyEligible.Should().BeTrue();
    }

    [Fact]
    public void Decision_matrix_path_existence_alone_is_DoNotSubmit()
    {
        var result = _sut.Analyze(
            "https://example.com/admin",
            200,
            "https://example.com/admin",
            [],
            "text/html",
            "<html><body>ok</body></html>");

        result.SubmissionRecommendation.Should().Be(SubmissionRecommendation.DoNotSubmit);
        result.ConfirmedVulnerability.Should().BeFalse();
        result.ManualReviewReasons.Should().BeEmpty();
    }

    [Fact]
    public void ManualReviewReasons_empty_forces_DoNotSubmit()
    {
        var result = _sut.Analyze(
            "https://example.com/admin",
            200,
            "https://example.com/admin",
            [],
            "text/html",
            "<html><body>ok</body></html>");

        result.ManualReviewReasons.Should().BeEmpty();
        result.SubmissionRecommendation.Should().Be(SubmissionRecommendation.DoNotSubmit);

        // Even if someone injects empty elevation path:
        var forced = result.WithAdditionalManualReviewReasons(Array.Empty<string>());
        forced.ManualReviewReasons.Should().BeEmpty();
        forced.SubmissionRecommendation.Should().Be(SubmissionRecommendation.DoNotSubmit);
    }

    [Fact]
    public void Surface_Evidence_rendered_exactly_once_in_hackerone_markdown()
    {
        var analysis = _sut.Analyze(
            url: "https://example.com/admin",
            httpStatusCode: 200,
            finalUrl: "https://example.com/admin",
            redirectChain: [],
            contentType: "text/html",
            body: """
                  <html><head><title>Admin Dashboard</title></head>
                  <body>
                    <h1>Administration Panel</h1>
                    <a>User Management</a><button>Delete User</button>
                    <section>Role assignment</section>
                  </body></html>
                  """);

        var surfaceBody = analysis.FormatSurfaceEvidence()
            .Replace("## Surface Evidence", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
        var steps = SensitiveSurfaceAnalyzer.BuildStepsFromSurfaceEvidence(
            analysis.FormatSurfaceEvidence(),
            "`https://example.com/admin`");

        var fields = new HackerOneReportDraftFields(
            Title: "AccessControlCandidate — unvalidated sensitive surface",
            Severity: "Unassigned",
            Asset: "example.com",
            Weakness: "Potential Weakness: CWE-284",
            Impact: "Candidate only. No unauthorized access demonstrated.",
            StepsToReproduce: steps,
            ProofOfConcept: "ManualReviewReasons:\n- Unauthenticated response contains administrative action controls.",
            Notes: "Manual Review",
            ConfirmedVulnerability: false,
            DemonstratedImpact: false,
            BugBountySeverityLabel: "Unassigned",
            Language: HackerOneReportLanguage.Code,
            FindingType: "AccessControlCandidate",
            SubmissionRecommendationLabel: "Manual Review",
            ActualResult: "See Surface Evidence / ManualReviewReasons.",
            SurfaceEvidence: surfaceBody);

        var md = _markdown.Build(fields);
        var count = CountOccurrences(md, "## Surface Evidence");
        count.Should().Be(1);
        md.Should().Contain("HTTP Status: 200");
        md.Should().Contain("Evidence Summary:");
        md.Should().Contain("Observed HTTP status `200`");
        md.Should().NotContain("Reproduce the described candidate behavior");
    }

    [Fact]
    public void Admin_public_landing_page_is_DoNotSubmit_not_Vulnerability()
    {
        var result = _sut.Analyze(
            url: "https://example.com/admin",
            httpStatusCode: 200,
            finalUrl: "https://example.com/admin",
            redirectChain: [],
            contentType: "text/html",
            body: """
                  <html><head><title>About Us</title></head>
                  <body><h1>Public landing</h1><p>Contact sales for a demo.</p></body></html>
                  """);

        result.SubmissionRecommendation.Should().Be(SubmissionRecommendation.DoNotSubmit);
        result.ConfirmedVulnerability.Should().BeFalse();
        result.DemonstratedImpact.Should().BeFalse();
        result.FindingClass.Should().Be(FindingClass.Informational);
        result.FindingClass.Should().NotBe(FindingClass.Vulnerability);
        result.Reason.Should().Contain("no unauthorized privileged access");
    }

    [Fact]
    public void Steps_are_built_from_recorded_http_observations()
    {
        var analysis = _sut.Analyze(
            url: "https://example.com/admin",
            httpStatusCode: 200,
            finalUrl: "https://example.com/login",
            redirectChain: ["https://example.com/admin → 302 → https://example.com/login"],
            contentType: "text/html",
            body: """
                  <html><head><title>Sign In</title></head>
                  <body><form><input type="password" name="password" /></form></body></html>
                  """);

        var steps = analysis.FormatStepsFromObservations();
        steps.Should().Contain("status `200`");
        steps.Should().Contain("https://example.com/login");
        steps.Should().Contain("LoginPageDetected=Yes");
        steps.Should().NotContain("Reproduce the described candidate behavior");

        var rebuilt = SensitiveSurfaceAnalyzer.BuildStepsFromSurfaceEvidence(
            analysis.FormatSurfaceEvidence(),
            "`https://example.com/admin`");
        rebuilt.Should().Contain("Observed HTTP status `200`");
        rebuilt.Should().Contain("Recorded redirect chain:");
    }

    private Finding Classify(SensitiveSurfaceAnalysisResult result)
    {
        var finding = new Finding
        {
            Title = result.FindingType,
            Description = result.Reason,
            Severity = Severity.Informational,
            CheckCode = "asc.access-control",
            Fingerprint = result.Fingerprint,
            CweCode = result.PotentialWeakness,
            Evidence = result.FormatSurfaceEvidence(),
            AffectedUrl = result.Url
        };
        _classifier.Classify(finding);
        return finding;
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            idx += needle.Length;
        }

        return count;
    }
}
