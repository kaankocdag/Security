using Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Infrastructure.AuthenticatedScanning;

public sealed class BrowserSessionService : IBrowserSessionService
{
    public Task<BrowserSessionHandle> CreateIsolatedAsync(
        string targetDomain,
        Guid testAccountId,
        bool headed,
        CancellationToken cancellationToken = default)
    {
        // Isolation is enforced by Playwright NewContext per (targetDomain, testAccountId).
        // Cookie/localStorage/sessionStorage never shared across domains or accounts.
        return Task.FromResult(new BrowserSessionHandle(targetDomain.Trim().ToLowerInvariant(), testAccountId, headed));
    }
}

public sealed class TestIdentityGenerator(ITestAccountVault vault) : ITestIdentityGenerator
{
    public string GenerateStrongPassword(int length = 24) => vault.GenerateStrongPassword(length);
}

public sealed class RegistrationFormFiller : IRegistrationFormFiller
{
    public IReadOnlyList<string> PlannedFillFieldNames(RegistrationFormAnalysis analysis) =>
        analysis.Fields
            .Where(f => f.Kind is RegistrationFormFieldKind.Email
                or RegistrationFormFieldKind.Username
                or RegistrationFormFieldKind.FirstName
                or RegistrationFormFieldKind.LastName
                or RegistrationFormFieldKind.DisplayName
                or RegistrationFormFieldKind.Password
                or RegistrationFormFieldKind.ConfirmPassword
                or RegistrationFormFieldKind.Country)
            .Select(f => f.Kind.ToString())
            .Distinct()
            .ToList();

    public bool ShouldAutoCheck(RegistrationFormFieldKind kind) =>
        // Never auto-check terms or newsletter/marketing.
        false;
}

public sealed class AutomatedLoginService : IAutomatedLoginService
{
    public bool IsCredentialDestinationAllowed(string targetDomain, string pageUrl, string? formActionHost)
    {
        var domain = targetDomain.Trim().TrimStart('.').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var page))
        {
            return false;
        }

        var pageHost = page.Host.TrimStart('.').ToLowerInvariant();
        if (!(pageHost == domain || pageHost.EndsWith("." + domain, StringComparison.Ordinal)))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(formActionHost))
        {
            return true;
        }

        var actionHost = formActionHost.Trim().TrimStart('.').ToLowerInvariant();
        return actionHost == domain || actionHost.EndsWith("." + domain, StringComparison.Ordinal);
    }
}

public sealed class AuthenticatedCrawlService : IAuthenticatedCrawlService
{
    private static readonly string[] Blocked =
    [
        "/logout", "/delete", "/remove", "/unsubscribe", "/billing", "/checkout",
        "/purchase", "/payment", "/invite", "/send", "/publish", "/cancel-account"
    ];

    public bool IsPathBlocked(string path) =>
        Blocked.Any(b => path.Contains(b, StringComparison.OrdinalIgnoreCase));

    public bool AllowsMethod(string method) =>
        method.Equals("GET", StringComparison.OrdinalIgnoreCase)
        || method.Equals("HEAD", StringComparison.OrdinalIgnoreCase);
}

public sealed class ScanSessionCleanupService : IScanSessionCleanupService
{
    public void ClearInMemorySecrets(ref string? password, ref string? cookieHeader)
    {
        password = null;
        cookieHeader = null;
    }
}

/// <summary>
/// Authenticated-only visibility / login-required is never auto-confirmed as a vulnerability.
/// </summary>
public static class AuthScanImpactRules
{
    public static (bool ConfirmedVulnerability, bool DemonstratedImpact, bool SubmissionEligible, bool PotentialRewardEligible)
        FromComparison(AuthScanComparisonResult comparison)
    {
        // Never auto-elevate from mode comparison alone.
        _ = comparison;
        return (false, false, false, false);
    }

    public static bool IsExpectedSecureMemberDenial(int authenticatedStatusCode, bool privilegedPath) =>
        privilegedPath && authenticatedStatusCode is 401 or 403 or 404;
}
