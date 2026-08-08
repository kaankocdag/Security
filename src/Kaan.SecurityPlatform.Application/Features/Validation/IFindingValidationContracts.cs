using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Validation.Dtos;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Entities.Validation;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Features.Validation;

public interface IFindingValidator
{
    string ValidatorType { get; }
    IReadOnlyList<string> SupportedFindingTypes { get; }
    ValidationAutomationKind AutomationKind { get; }
    ValidationRiskLevel RiskLevel { get; }
    ValidationMode DefaultMode { get; }
    bool RequiresUserApproval { get; }

    bool CanHandle(Finding finding);
    Task<ValidationPreconditionResult> CheckPreconditionsAsync(
        ValidationContext context,
        CancellationToken cancellationToken = default);

    Task<ValidatorExecutionResult> ValidateAsync(
        ValidationContext context,
        IValidationHttpGate httpGate,
        CancellationToken cancellationToken = default);
}

public interface IValidatorRegistry
{
    IFindingValidator Resolve(Finding finding);
    IReadOnlyList<IFindingValidator> All { get; }
}

public interface IFindingValidationOrchestrator
{
    Task<Result<ValidationPreconditionsDto>> GetPreconditionsAsync(
        Guid findingId,
        CancellationToken cancellationToken = default);

    Task<Result<FindingValidationRunDto>> StartAsync(
        StartFindingValidationRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<FindingValidationRunDto>> GetRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<Result<FindingValidationRunDto>> StopAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FindingValidationRunDto>> ListRunsForFindingAsync(
        Guid findingId,
        CancellationToken cancellationToken = default);
}

public interface IValidationPolicyEngine
{
    Task<PolicyDecision> EvaluateAsync(ValidationContext context, CancellationToken cancellationToken = default);
}

public interface IScopePolicyValidator
{
    Task<ScopePolicy?> GetActiveAsync(Guid targetId, CancellationToken cancellationToken = default);
    bool IsMethodAllowed(ScopePolicy policy, string method);
    bool IsTestTypeAllowed(ScopePolicy policy, string testType);
}

public interface IAuthorizationEvidenceService
{
    Task<ValidationAuthorizationEvidence?> GetActiveAsync(Guid targetId, CancellationToken cancellationToken = default);
    Task<Result<ValidationAuthorizationEvidenceDto>> UpsertAsync(
        UpsertAuthorizationEvidenceRequest request,
        CancellationToken cancellationToken = default);
}

public interface IValidationRunService
{
    Task<FindingValidationRun> CreateAwaitingApprovalAsync(
        Finding finding,
        Guid targetId,
        IFindingValidator validator,
        Guid? requestedBy,
        CancellationToken cancellationToken = default);

    Task MarkRunningAsync(FindingValidationRun run, CancellationToken cancellationToken = default);
    Task CompleteAsync(FindingValidationRun run, FindingValidationResult result, CancellationToken cancellationToken = default);
}

public interface IEvidenceCollector
{
    ValidationEvidence CreateHttpEvidence(
        Guid runId,
        ValidationSessionRole role,
        string method,
        string url,
        int status,
        string? finalUrl,
        IReadOnlyList<string> redirectChain,
        string? contentType,
        string? bodyExcerpt,
        string? responseHash);
}

public interface IEvidenceRedactor
{
    string RedactUrl(string? url);
    string RedactBody(string? body);
    string HashBody(string? body);
}

public interface IImpactAssessmentService
{
    ImpactAssessment Assess(ValidatorExecutionResult execution);
}

public interface ISubmissionEligibilityEvaluator
{
    EligibilityDecision Evaluate(ImpactAssessment impact, PolicyDecision policy, ValidatorExecutionResult execution);
}

public interface IValidationAuditService
{
    Task WriteAsync(string action, Guid? runId, object? details, CancellationToken cancellationToken = default);
}

public interface IValidationHttpGate
{
    Task<ValidationHttpResponse> SendSafeAsync(
        FindingValidationRun run,
        HttpMethod method,
        Uri url,
        ValidationSessionRole role,
        string? authorizationHeaderValue,
        CancellationToken cancellationToken = default);

    bool StopRequested { get; }
}

public interface ITestAccountSecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedPayload);
}

public interface IValidationCatalogService
{
    Task<Result<ScopePolicy>> UpsertScopeAsync(UpsertScopePolicyRequest request, CancellationToken cancellationToken = default);
    Task<Result<Guid>> UpsertTestAccountAsync(UpsertTestAccountRequest request, CancellationToken cancellationToken = default);
}

public sealed record ValidationContext(
    Finding Finding,
    Guid TargetId,
    string TargetHost,
    Uri? AffectedUri,
    ScopePolicy? ScopePolicy,
    ValidationAuthorizationEvidence? AuthorizationEvidence,
    IReadOnlyList<TestAccountSession> TestAccounts,
    FindingValidationRun Run,
    bool ExplicitUserApproval,
    bool IsDevelopmentEnvironment,
    string? OwnedTestResourceUrl);

public sealed record ValidationPreconditionResult(
    bool CanStart,
    ValidationStatus SuggestedStatus,
    IReadOnlyList<string> MissingItems,
    ValidationAutomationKind AutomationKind,
    ValidationRiskLevel RiskLevel,
    string ValidatorType);

public sealed record ValidatorExecutionResult(
    ValidationStatus Status,
    bool ConfirmedVulnerability,
    bool DemonstratedImpact,
    ValidationImpactType ImpactType,
    ValidationConfidence Confidence,
    string ExpectedResult,
    string ActualResult,
    IReadOnlyList<string> ManualReviewReasons,
    IReadOnlyList<ValidationEvidence> Evidence,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    int ReproductionCount = 0,
    string? TestAccountRolesUsed = null);

public sealed record PolicyDecision(
    bool Allowed,
    bool TargetInBountyScope,
    bool TestingMethodAllowed,
    bool AuthorizationValid,
    string? BlockReason,
    ValidationStatus? BlockStatus);

public sealed record ImpactAssessment(
    bool ConfirmedVulnerability,
    bool DemonstratedImpact,
    ValidationImpactType ImpactType,
    ValidationConfidence Confidence);

public sealed record EligibilityDecision(
    ValidationSubmissionRecommendation Recommendation,
    bool SubmissionEligible,
    bool PotentialRewardEligible,
    string EligibilityReason);

public sealed record ValidationHttpResponse(
    int StatusCode,
    string FinalUrl,
    IReadOnlyList<string> RedirectChain,
    string? ContentType,
    string Body,
    bool RateLimited,
    bool ServerErrorSpike);
