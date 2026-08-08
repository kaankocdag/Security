using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Features.Validation.Dtos;

public sealed record ValidationPreconditionsDto(
    Guid FindingId,
    string ValidatorType,
    ValidationAutomationKind AutomationKind,
    ValidationRiskLevel RiskLevel,
    bool CanStartAutomatic,
    bool ManualOnly,
    IReadOnlyList<string> MissingItems,
    bool TargetInBountyScope,
    bool TestingMethodAllowed,
    bool AuthorizationValid,
    bool HasScopePolicy,
    bool HasAuthorizationEvidence,
    string Disclaimer);

public sealed record StartFindingValidationRequest(
    Guid FindingId,
    bool ExplicitUserApproval,
    string? OwnedTestResourceUrl = null,
    Guid? TestAccountAId = null,
    Guid? TestAccountBId = null);

public sealed record FindingValidationRunDto(
    Guid Id,
    Guid FindingId,
    Guid TargetId,
    string ValidatorType,
    ValidationMode ValidationMode,
    ValidationStatus Status,
    ValidationRiskLevel RiskLevel,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int MaxRequestCount,
    int ActualRequestCount,
    string? StopReason,
    string? ErrorCode,
    string? ErrorMessage,
    FindingValidationResultDto? Result,
    IReadOnlyList<ValidationEvidenceDto> Evidence);

public sealed record FindingValidationResultDto(
    bool ConfirmedVulnerability,
    bool DemonstratedImpact,
    ValidationImpactType ImpactType,
    ValidationConfidence Confidence,
    ValidationSubmissionRecommendation SubmissionRecommendation,
    bool SubmissionEligible,
    bool PotentialRewardEligible,
    string? EligibilityReason,
    IReadOnlyList<string> ManualReviewReasons,
    string? ExpectedResult,
    string? ActualResult,
    string ValidatorVersion,
    int ReproductionCount,
    string? TestAccountRolesUsed,
    string RewardDisclaimer);

public sealed record ValidationEvidenceDto(
    Guid Id,
    ValidationEvidenceType EvidenceType,
    string RequestMethod,
    string? RedactedRequestUrl,
    int? ResponseStatusCode,
    string? FinalUrl,
    string? RedirectChain,
    string? ResponseContentType,
    string? ResponseHash,
    string? RedactedResponseExcerpt,
    ValidationSessionRole SessionRole,
    DateTime CapturedAt);

public sealed record ValidationAuthorizationEvidenceDto(
    Guid Id,
    Guid TargetId,
    string AuthorizedByName,
    string AuthorizedByEmail,
    string ScopeSummary,
    string AllowedTestTypes,
    DateTime ValidFrom,
    DateTime ValidUntil,
    bool IsActive);

public sealed record UpsertAuthorizationEvidenceRequest(
    Guid TargetId,
    string AuthorizedByName,
    string AuthorizedByEmail,
    string ScopeSummary,
    string AllowedTestTypes,
    DateTime ValidFrom,
    DateTime ValidUntil,
    string? EvidenceNotes,
    Guid? AuthorizationRecordId);

public sealed record UpsertScopePolicyRequest(
    Guid TargetId,
    string ProgramName,
    string? ProgramUrl,
    ScopePolicyStatus ScopeStatus,
    string AllowedTestMethods,
    string ProhibitedTestMethods,
    int RateLimit,
    DateTime? ValidFrom,
    DateTime? ValidUntil,
    bool TargetInBountyScope,
    string? PolicyEvidence);

public sealed record UpsertTestAccountRequest(
    Guid TargetId,
    string Label,
    ValidationSessionRole Role,
    bool OwnershipConfirmed,
    bool TestingPermissionConfirmed,
    string SecretMaterial,
    string? OwnedTestResourceHint);
