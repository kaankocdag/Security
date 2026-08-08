using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning.Dtos;

public sealed record TargetTestAccountDto(
    Guid Id,
    Guid TargetId,
    string TargetDomain,
    string Label,
    string? Email,
    string? Username,
    string? DisplayName,
    TestAccountStatus AccountStatus,
    TestAccountVerificationStatus VerificationStatus,
    string? RegistrationUrl,
    string? LoginUrl,
    DateTime? LastSuccessfulLoginAt,
    DateTime? LastAuthenticatedScanAt,
    bool OwnershipConfirmed,
    bool TestingPermissionConfirmed,
    bool IsActive,
    ValidationSessionRole Role,
    string? Notes);

public sealed record UpsertTestIdentityProfileRequest(
    Guid TargetId,
    string ProfileName,
    string Email,
    string? Username,
    string? FirstName,
    string? LastName,
    string? DisplayName,
    string? Country,
    DateOnly? BirthDate,
    string? ProgramName,
    string? ProgramUrl,
    bool OwnershipConfirmed,
    bool TestingPermissionConfirmed);

public sealed record RegisterExistingTestAccountRequest(
    Guid TargetId,
    string Label,
    string Email,
    string? Username,
    string? DisplayName,
    string Password,
    string? LoginUrl,
    ValidationSessionRole Role,
    bool OwnershipConfirmed,
    bool TestingPermissionConfirmed);

public sealed record RegistrationPlanDto(
    Guid TargetId,
    Guid IdentityProfileId,
    string? DetectedRegistrationUrl,
    IReadOnlyList<string> FieldsToFill,
    IReadOnlyList<string> ManualStepsRequired,
    ManualTakeoverReason BlockReason,
    string Disclaimer,
    bool CanFillWithoutSubmit);

public sealed record ConfirmRegistrationSubmitRequest(
    Guid TargetId,
    Guid IdentityProfileId,
    string RegistrationUrl,
    bool ExplicitSubmitApproval,
    string? GeneratedPasswordVaultReference);

public sealed record StartAuthenticatedScanRequest(
    Guid TargetId,
    Guid TestAccountId,
    bool ExplicitUserApproval,
    bool HeadedBrowser = true,
    IReadOnlyList<string>? PathsToProbe = null);

/// <summary>
/// Şifresiz akış: görünür tarayıcı açılır, kullanıcı girişi kendisi yapar
/// (Google/SSO/MFA dahil), ardından yalnızca GET probe'ları çalışır.
/// </summary>
public sealed record StartManualLoginSessionRequest(
    Guid TargetId,
    string? LoginUrl,
    bool ExplicitUserApproval,
    bool RunAnonymousBaseline = true);

/// <summary>
/// En kolay yol: kullanıcı kendi normal tarayıcısında giriş yapar, oturum
/// çerezini (ham "ad=değer; ad2=değer2" başlığı veya Cookie-Editor JSON dışa
/// aktarımı) yapıştırır. Otomasyon/Playwright yok, "güvenli değil" hatası yok.
/// Çerez saklanmaz; yalnızca bu koşu için bellekte tutulur.
/// </summary>
public sealed record StartCookieSessionScanRequest(
    Guid TargetId,
    string CookieData,
    bool ExplicitUserApproval,
    bool RunAnonymousBaseline = true);

public sealed record LoginDiscoveryDto(
    Guid TargetId,
    string? BestLoginUrl,
    IReadOnlyList<string> CandidateUrls,
    bool PasswordFormDetected,
    bool OAuthOnlyLikely,
    IReadOnlyList<string> OAuthProviders,
    string Note);

public sealed record AuthScanPreconditionsDto(
    Guid TargetId,
    bool HasScopePolicy,
    bool HasAuthorizationEvidence,
    bool TargetInBountyScope,
    bool AutoRegistrationAllowed,
    int ActiveTestAccountCount,
    int MaxTestAccounts,
    IReadOnlyList<string> MissingItems,
    string Disclaimer);

public sealed record ScanModeObservationDto(
    bool IsAuthenticatedMode,
    string? MaskedAccountLabel,
    string Url,
    int StatusCode,
    string? FinalUrl,
    string? RedirectChain,
    string? ContentType,
    string? ResponseHash,
    bool LoginDetected,
    bool AccessDeniedDetected,
    bool AuthenticationConfirmed,
    string? RedactedEvidence,
    AuthScanComparisonResult? ComparisonResult);

public sealed record AuthenticatedScanRunDto(
    Guid Id,
    Guid TargetId,
    Guid? TestAccountId,
    string? MaskedAccountLabel,
    AuthenticatedScanRunStatus Status,
    ManualTakeoverReason TakeoverReason,
    string? TakeoverMessage,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int MaxRequestCount,
    int ActualRequestCount,
    string? StopReason,
    bool AuthenticationConfirmed,
    string? LoginUrlUsed,
    bool BrowserSessionHeld,
    string? ErrorCode,
    string? ErrorMessage,
    IReadOnlyList<ScanModeObservationDto> AnonymousObservations,
    IReadOnlyList<ScanModeObservationDto> AuthenticatedObservations,
    IReadOnlyList<ScanModeObservationDto> Comparisons);
