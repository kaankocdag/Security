using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.HackerOne.Dtos;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Features.HackerOne;

public interface IHackerOneWorkspaceService
{
    Task<HackerOneOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HackerOneCandidateDto>> ListCandidatesAsync(
        SubmissionRecommendation? recommendation = null,
        string? programPolicyKey = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BugBountyProgramDto>> ListProgramsAsync(CancellationToken cancellationToken = default);
    Task<Result<BugBountyProgramDto>> GetProgramAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<BugBountyProgramDto>> UpdateProgramEnabledAsync(Guid id, bool isEnabled, CancellationToken cancellationToken = default);

    Task<HackerOneWorkspaceSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<Result<HackerOneWorkspaceSettingsDto>> UpdateSettingsAsync(UpdateHackerOneWorkspaceSettingsRequest request, CancellationToken cancellationToken = default);
    Task<Result> SetApiTokenAsync(SetHackerOneApiTokenRequest request, CancellationToken cancellationToken = default);
    Task<Result> ClearApiTokenAsync(CancellationToken cancellationToken = default);

    Task<Result<int>> SyncProgramsAsync(CancellationToken cancellationToken = default);

    /// <summary>Sync all HackerOne program structured scopes into Domains (background-friendly).</summary>
    Task<Result<HackerOneScopeSyncResultDto>> SyncScopesToDomainsAsync(CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının elle eklediği yetkili bir test hedefini (doğrulanmış) targets listesine ekler.</summary>
    Task<Result<Guid>> AddManualTargetAsync(string hostName, bool authorizedConfirmed, CancellationToken cancellationToken = default);

    /// <summary>Hedef için en son ASC sonucunu (motor listesi + bulgular) döner — bulgu olmasa da özet gelir.</summary>
    Task<Result<TargetAssessmentSummaryDto>> GetLatestAssessmentSummaryAsync(
        Guid domainAssetId,
        CancellationToken cancellationToken = default);

    Task<Result<HackerOneReportDraftDto>> CreateOrGetDraftAsync(CreateHackerOneDraftRequest request, CancellationToken cancellationToken = default);
    Task<Result<HackerOneReportDraftDto>> GetDraftAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HackerOneReportDraftDto>> ListDraftsAsync(CancellationToken cancellationToken = default);
    Task<Result<HackerOneReportDraftDto>> UpdateDraftAsync(Guid id, UpdateHackerOneDraftRequest request, CancellationToken cancellationToken = default);
    Task<Result<HackerOneMarkdownDto>> GetMarkdownAsync(
        Guid id,
        string? language = null,
        CancellationToken cancellationToken = default);
    Task<Result<HackerOneMarkdownDto>> RecalculateReadinessAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HackerOneSubmissionDto>> ListSubmissionsAsync(CancellationToken cancellationToken = default);
    Task<Result<HackerOneSubmissionDto>> SubmitDraftAsync(Guid draftId, SubmitHackerOneDraftRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScanProfileDto>> ListScanProfilesAsync(CancellationToken cancellationToken = default);
    Task<Result<Guid>> StartCandidateAssessmentAsync(StartCandidateAssessmentRequest request, CancellationToken cancellationToken = default);
}

public interface IHackerOneMarkdownBuilder
{
    /// <summary>HackerOne export (en-US).</summary>
    string Build(HackerOneReportDraftFields fields, bool preferEnglish = true);

    /// <summary>Internal Turkish review report — not for HackerOne submit.</summary>
    string BuildTurkish(HackerOneReportDraftFields fields);

    int ComputeReadinessScore(HackerOneReportDraftFields fields);
    string FormatSafeUrlForSteps(string? urlOrHost);
}

public sealed record HackerOneReportDraftFields(
    string Title,
    string Severity,
    string Asset,
    string Weakness,
    string Impact,
    string StepsToReproduce,
    string ProofOfConcept,
    string? Notes,
    bool ConfirmedVulnerability = false,
    bool DemonstratedImpact = false,
    string BugBountySeverityLabel = "Unassigned",
    string Language = HackerOneReportLanguage.Code,
    string? FindingType = null,
    string? CandidateSeverity = null,
    string? ExploitabilityLabel = null,
    string? SubmissionRecommendationLabel = null,
    string? Summary = null,
    string? VulnerabilityInformation = null,
    string? ExpectedResult = null,
    string? ActualResult = null,
    string? SuggestedRemediation = null,
    string? TestingNotes = null,
    string? EligibilityReason = null,
    /// <summary>Rendered once in markdown under ## Surface Evidence. Do not duplicate in Steps/PoC.</summary>
    string? SurfaceEvidence = null,
    /// <summary>Rendered once under ## Validation Evidence.</summary>
    string? ValidationEvidence = null);

public interface IHackerOneApiClient
{
    bool IsEnabled { get; }
    Task<Result<IReadOnlyList<HackerOneRemoteProgram>>> ListProgramsAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<HackerOneRemoteScope>>> ListStructuredScopesAsync(string programHandle, CancellationToken cancellationToken = default);
    Task<Result<HackerOneRemoteSubmission>> SubmitReportAsync(HackerOneSubmitPayload payload, CancellationToken cancellationToken = default);
}

public sealed record HackerOneRemoteProgram(
    string ExternalId,
    string Handle,
    string Name,
    bool OffersBounties = false,
    string? Currency = null,
    string? SubmissionState = null,
    bool OpenScope = false,
    string? State = null);

public sealed record HackerOneRemoteScope(
    string ExternalId,
    string AssetIdentifier,
    string AssetType,
    bool EligibleForBounty,
    bool EligibleForSubmission,
    string? MaxSeverity,
    string? Instruction);

public sealed record HackerOneRemoteSubmission(string ExternalReportId, string ExternalReportUrl);

public sealed record HackerOneScopeSyncResultDto(
    int ProgramsProcessed,
    int ScopesSeen,
    int DomainsUpserted,
    int DomainsSkipped,
    string Message);
public sealed record HackerOneSubmitPayload(
    string ProgramHandle,
    string Title,
    string Severity,
    string MarkdownBody);

public interface IHackerOneSecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedPayload);
}

public interface IBugBountyAuditWriter
{
    Task WriteAsync(string action, string entityType, string? entityId, object? details = null, CancellationToken cancellationToken = default);
}

public interface IRootCauseGroupService
{
    Task<Guid> AssignAsync(Guid findingId, string? fingerprint, string title, CancellationToken cancellationToken = default);
}

public interface IApplicationSecurityCandidateEngine
{
    string EngineKey { get; }
    Task<IReadOnlyList<CandidateFindingDraft>> RunAsync(CandidateEngineContext context, CancellationToken cancellationToken = default);
}

public sealed record CandidateEngineContext(
    Guid CompanyId,
    Guid ScanResultId,
    string TargetHost,
    Uri BaseUri,
    string UserAgent,
    string? TestAccountUsername,
    string? TestAccountPassword);

public sealed record CandidateFindingDraft(
    string Title,
    string Description,
    string CheckCode,
    string Fingerprint,
    Severity Severity,
    string Category,
    string? AffectedUrl,
    string? AffectedParameter,
    string? Evidence,
    string? Remediation,
    string? CweCode,
    string? OwaspCategory,
    CandidateReflectionMetadata? Reflection = null);

public sealed record CandidateReflectionMetadata(
    ReflectionContext Context,
    int ReflectionCount,
    bool HtmlEncoded,
    bool AttributeEncoded,
    string? ContentType,
    int HttpStatus,
    string? ReflectionLocation,
    string InputSource,
    string Marker,
    bool ProperlyEncoded);
