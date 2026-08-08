using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning.Dtos;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning;

public interface IAuthenticatedScanOrchestrator
{
    Task<Result<AuthScanPreconditionsDto>> GetPreconditionsAsync(Guid targetId, CancellationToken cancellationToken = default);
    Task<Result<AuthenticatedScanRunDto>> StartAuthenticatedScanAsync(StartAuthenticatedScanRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthenticatedScanRunDto>> StartManualLoginSessionAsync(StartManualLoginSessionRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthenticatedScanRunDto>> StartCookieSessionScanAsync(StartCookieSessionScanRequest request, CancellationToken cancellationToken = default);
    Task<Result<LoginDiscoveryDto>> DiscoverLoginAsync(Guid targetId, CancellationToken cancellationToken = default);
    Task<Result<AuthenticatedScanRunDto>> GetRunAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<Result<AuthenticatedScanRunDto>> StopAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<Result<AuthenticatedScanRunDto>> ContinueAfterManualTakeoverAsync(Guid runId, CancellationToken cancellationToken = default);
}

public interface ILoginPageDiscoveryService
{
    IReadOnlyList<string> ExtractLoginLinks(string html, string pageUrl);
    IReadOnlyList<string> DetectOAuthProviders(string html);
    bool HasPasswordForm(string html);
}

public interface ITestAccountVault
{
    string ProtectPassword(string password);
    string UnprotectPassword(string encryptedReference);
    string GenerateStrongPassword(int length = 24);
}

public interface ITestAccountManagementService
{
    Task<IReadOnlyList<TargetTestAccountDto>> ListAsync(Guid targetId, CancellationToken cancellationToken = default);
    Task<Result<TargetTestAccountDto>> RegisterExistingAsync(RegisterExistingTestAccountRequest request, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateIdentityProfileAsync(UpsertTestIdentityProfileRequest request, CancellationToken cancellationToken = default);
    Task<Result<RegistrationPlanDto>> PlanRegistrationAsync(Guid targetId, Guid identityProfileId, CancellationToken cancellationToken = default);
    Task<Result<TargetTestAccountDto>> ConfirmRegistrationSubmitAsync(ConfirmRegistrationSubmitRequest request, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(Guid accountId, string newPassword, CancellationToken cancellationToken = default);
    Task<Result> DisableAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<Result> DeleteVaultAsync(Guid accountId, CancellationToken cancellationToken = default);
    Task<Result<string>> RevealPasswordAsync(Guid accountId, bool forCopy, CancellationToken cancellationToken = default);
}

public interface IBrowserSessionService
{
    Task<BrowserSessionHandle> CreateIsolatedAsync(string targetDomain, Guid testAccountId, bool headed, CancellationToken cancellationToken = default);
}

public interface IBrowserSessionHoldService
{
    bool IsHeld(Guid runId);
    Task ReleaseAsync(Guid runId);
}

public interface ITestIdentityGenerator
{
    string GenerateStrongPassword(int length = 24);
}

public interface IRegistrationFormFiller
{
    IReadOnlyList<string> PlannedFillFieldNames(RegistrationFormAnalysis analysis);
    bool ShouldAutoCheck(RegistrationFormFieldKind kind);
}

public interface IAutomatedLoginService
{
    bool IsCredentialDestinationAllowed(string targetDomain, string pageUrl, string? formActionHost);
}

public interface IAuthenticatedCrawlService
{
    bool IsPathBlocked(string path);
    bool AllowsMethod(string method);
}

public interface IScanSessionCleanupService
{
    void ClearInMemorySecrets(ref string? password, ref string? cookieHeader);
}

public sealed record BrowserSessionHandle(string TargetDomain, Guid TestAccountId, bool Headed);

public interface ILoginPageDetector
{
    bool LooksLikeLoginPage(string? url, string? html, string? title);
    string? SuggestLoginPath(IEnumerable<string> discoveredPaths);
}

public interface IRegistrationPageDetector
{
    bool LooksLikeRegistrationPage(string? url, string? html, string? title);
    IReadOnlyList<string> CandidatePaths { get; }
}

public interface IRegistrationFormAnalyzer
{
    RegistrationFormAnalysis Analyze(string html, string pageUrl);
}

public interface ILoginFormAnalyzer
{
    LoginFormAnalysis Analyze(string html, string pageUrl);
}

public interface IAuthenticationStateDetector
{
    bool IsAuthenticated(string? url, string? html, int statusCode);
    bool IsAuthRequired(string? url, string? html, int statusCode, IReadOnlyList<string> redirectChain);
}

public interface IAuthenticatedEvidenceRedactor
{
    string Redact(string? value);
    string Hash(string? value);
}

public interface IManualTakeoverService
{
    ManualTakeoverReason Detect(string? url, string? html);
    string UserMessage(ManualTakeoverReason reason);
}

public sealed record RegistrationFormField(RegistrationFormFieldKind Kind, string SelectorHint, bool Required, string? Label);

public sealed record RegistrationFormAnalysis(
    bool FormFound,
    string? FormAction,
    string FormActionHost,
    IReadOnlyList<RegistrationFormField> Fields,
    ManualTakeoverReason BlockReason,
    bool HasNewsletterConsent,
    bool HasTermsAcceptance);

public sealed record LoginFormAnalysis(
    bool FormFound,
    string? FormAction,
    string FormActionHost,
    bool HasPassword,
    bool HasUsernameOrEmail,
    ManualTakeoverReason BlockReason);
