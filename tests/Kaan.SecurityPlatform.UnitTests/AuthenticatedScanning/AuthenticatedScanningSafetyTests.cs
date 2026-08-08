using FluentAssertions;
using Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning;
using Kaan.SecurityPlatform.Application.Features.Validation;
using Kaan.SecurityPlatform.Domain.Entities.AuthenticatedScanning;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.AuthenticatedScanning;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.AuthenticatedScanning;

public sealed class AuthenticatedScanningSafetyTests
{
    private readonly ManualTakeoverService _takeover = new();
    private readonly RegistrationFormAnalyzer _regForm = new();
    private readonly RegistrationFormFiller _filler = new();
    private readonly AuthenticatedEvidenceRedactor _redactor = new();
    private readonly AutomatedLoginService _loginGate = new();
    private readonly AuthenticatedCrawlService _crawl = new();
    private readonly LoginPageDetector _loginPage = new();

    [Fact]
    public void Captcha_stops_automation()
    {
        _takeover.Detect("https://ex.com/register", "<div class='g-recaptcha'></div>")
            .Should().Be(ManualTakeoverReason.Captcha);
        _takeover.UserMessage(ManualTakeoverReason.Captcha)
            .Should().Contain("Manuel işlem gerekli");
    }

    [Fact]
    public void Mfa_stops_automation()
    {
        _takeover.Detect("https://ex.com/login", "Enter your two-factor code / MFA")
            .Should().Be(ManualTakeoverReason.Mfa);
    }

    [Fact]
    public void Payment_screen_stops_automation()
    {
        _takeover.Detect("https://ex.com/billing", "Enter credit card to subscribe now")
            .Should().Be(ManualTakeoverReason.PaymentOrSubscription);
    }

    [Fact]
    public void Terms_and_newsletter_are_never_auto_checked()
    {
        var html = """
            <form action="/register">
              <label for="email">Email</label><input id="email" name="email" type="email" required />
              <input name="password" type="password" required />
              <input name="terms" type="checkbox" required aria-label="I agree to terms" />
              <input name="newsletter" type="checkbox" aria-label="newsletter marketing" />
            </form>
            """;
        var analysis = _regForm.Analyze(html, "https://example.com/register");
        analysis.HasTermsAcceptance.Should().BeTrue();
        analysis.HasNewsletterConsent.Should().BeTrue();
        analysis.BlockReason.Should().Be(ManualTakeoverReason.TermsAcceptance);
        _filler.ShouldAutoCheck(RegistrationFormFieldKind.TermsAcceptance).Should().BeFalse();
        _filler.ShouldAutoCheck(RegistrationFormFieldKind.NewsletterConsent).Should().BeFalse();
    }

    [Fact]
    public void Unexpected_domain_blocks_credentials()
    {
        _loginGate.IsCredentialDestinationAllowed("example.com", "https://evil.com/login", "evil.com")
            .Should().BeFalse();
        _loginGate.IsCredentialDestinationAllowed("example.com", "https://example.com/login", "example.com")
            .Should().BeTrue();
    }

    [Fact]
    public void Logout_and_delete_paths_are_blocked()
    {
        _crawl.IsPathBlocked("/account/logout").Should().BeTrue();
        _crawl.IsPathBlocked("/user/delete").Should().BeTrue();
        _crawl.IsPathBlocked("/dashboard").Should().BeFalse();
        _crawl.AllowsMethod("POST").Should().BeFalse();
        _crawl.AllowsMethod("GET").Should().BeTrue();
    }

    [Fact]
    public void Redactor_strips_password_cookie_token()
    {
        var redacted = _redactor.Redact("password=SuperSecret123! Cookie: session=abc Authorization: Bearer tok.en");
        redacted.Should().NotContain("SuperSecret");
        redacted.Should().NotContain("session=abc");
        redacted.Should().NotContain("Bearer tok");
        redacted.Should().Contain("[redacted]");
    }

    [Fact]
    public void Login_required_is_not_vulnerability()
    {
        var anon = new ScanModeObservation
        {
            LoginDetected = true,
            StatusCode = 302,
            ResponseHash = "aaa"
        };
        var auth = new ScanModeObservation
        {
            AuthenticationConfirmed = true,
            StatusCode = 200,
            ResponseHash = "bbb"
        };
        var cmp = AuthenticatedScanOrchestrator.Compare(anon, auth);
        cmp.Should().Be(AuthScanComparisonResult.LoginRequired);
        var impact = AuthScanImpactRules.FromComparison(cmp);
        impact.ConfirmedVulnerability.Should().BeFalse();
        impact.DemonstratedImpact.Should().BeFalse();
        impact.SubmissionEligible.Should().BeFalse();
        impact.PotentialRewardEligible.Should().BeFalse();
    }

    [Fact]
    public void Member_admin_403_is_expected_secure()
    {
        AuthScanImpactRules.IsExpectedSecureMemberDenial(403, privilegedPath: true).Should().BeTrue();
        var anon = new ScanModeObservation { StatusCode = 401, AccessDeniedDetected = true, ResponseHash = "x" };
        var auth = new ScanModeObservation { StatusCode = 403, AccessDeniedDetected = true, ResponseHash = "y" };
        AuthenticatedScanOrchestrator.Compare(anon, auth).Should().Be(AuthScanComparisonResult.AccessDeniedAsExpected);
        var impact = AuthScanImpactRules.FromComparison(AuthScanComparisonResult.AccessDeniedAsExpected);
        impact.SubmissionEligible.Should().BeFalse();
    }

    [Fact]
    public void Auth_required_signals_detected_on_login_page()
    {
        var detector = new AuthenticationStateDetector();
        detector.IsAuthRequired(
            "https://example.com/login",
            """<html><body>Sign in <input type="password"/></body></html>""",
            200,
            ["https://example.com/admin", "https://example.com/login"]).Should().BeTrue();
        _loginPage.LooksLikeLoginPage("https://ex.com/signin", "<input type='password'/> Log in", "Sign in")
            .Should().BeTrue();
    }

    [Fact]
    public void Strong_password_meets_policy()
    {
        var vault = new TestAccountVault(new FakeProtector());
        var pwd = vault.GenerateStrongPassword(24);
        pwd.Length.Should().BeGreaterThanOrEqualTo(20);
        pwd.Any(char.IsUpper).Should().BeTrue();
        pwd.Any(char.IsLower).Should().BeTrue();
        pwd.Any(char.IsDigit).Should().BeTrue();
        pwd.Any(c => "!@#$%^&*-_=+".Contains(c)).Should().BeTrue();
    }

    [Fact]
    public void Browser_contexts_are_keyed_per_domain_and_account()
    {
        var svc = new BrowserSessionService();
        var a = svc.CreateIsolatedAsync("Example.COM", Guid.Parse("11111111-1111-1111-1111-111111111111"), true).Result;
        var b = svc.CreateIsolatedAsync("other.com", Guid.Parse("22222222-2222-2222-2222-222222222222"), true).Result;
        a.TargetDomain.Should().Be("example.com");
        b.TargetDomain.Should().Be("other.com");
        a.TestAccountId.Should().NotBe(b.TestAccountId);
    }

    private sealed class FakeProtector : ITestAccountSecretProtector
    {
        public string Protect(string plaintext) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plaintext));
        public string Unprotect(string protectedPayload) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(protectedPayload));
    }
}
