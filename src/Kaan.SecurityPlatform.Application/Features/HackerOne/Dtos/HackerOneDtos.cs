using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Features.HackerOne.Dtos;

public sealed record HackerOneOverviewDto(
    int CandidateCount,
    int SubmitRecommendedCount,
    int ManualReviewCount,
    int DraftCount,
    int ReadyDraftCount,
    int SubmissionCount,
    int BugBountyEligibleCount,
    bool ApiEnabled,
    string? DefaultProgramHandle);

public sealed record HackerOneCandidateDto(
    Guid FindingId,
    string Title,
    Severity TechnicalSeverity,
    FindingClass FindingClass,
    SubmissionRecommendation SubmissionRecommendation,
    bool BugBountyEligible,
    bool DemonstratedImpact,
    bool ConfirmedVulnerability,
    bool SubmissionEligible,
    bool PotentialRewardEligible,
    ValidationStatus? LatestValidationStatus,
    string? ProgramPolicyMatch,
    string? DomainHostName,
    string? AffectedUrl,
    string? Fingerprint,
    Guid? RootCauseGroupId,
    string? EligibilityReason,
    DateTime LastSeenAt);

public sealed record BugBountyProgramDto(
    Guid Id,
    string PolicyKey,
    string Name,
    string Handle,
    BugBountyPlatform Platform,
    string? OpenReportUrl,
    bool IsEnabled,
    DateTime? LastSyncedAt,
    IReadOnlyList<BugBountyPolicyRuleDto> Rules,
    bool OffersBounties = false,
    string? Currency = null,
    string? SubmissionState = null,
    bool OpenScope = false,
    string? State = null);

public sealed record BugBountyPolicyRuleDto(
    Guid Id,
    BugBountyPolicyCategory PolicyCategory,
    SubmissionRecommendation RecommendationWhenDemonstrated,
    SubmissionRecommendation RecommendationWhenNotDemonstrated,
    string? Notes);

public sealed record HackerOneWorkspaceSettingsDto(
    Guid Id,
    Guid? DefaultBugBountyProgramId,
    string OpenReportUrlTemplate,
    int MinReadinessScoreForSubmit,
    bool PreferEnglishReports,
    bool ApiEnabled,
    bool HasApiToken,
    bool HasApiTokenIdentifier,
    string? ApiTokenIdentifierHint);

public sealed record UpdateHackerOneWorkspaceSettingsRequest(
    Guid? DefaultBugBountyProgramId,
    string? OpenReportUrlTemplate,
    int? MinReadinessScoreForSubmit,
    bool? PreferEnglishReports);

public sealed record SetHackerOneApiTokenRequest(string ApiToken, string? ApiUsername);

public sealed record HackerOneReportDraftDto(
    Guid Id,
    Guid FindingId,
    Guid BugBountyProgramId,
    string ProgramHandle,
    string Title,
    string Severity,
    string Asset,
    string Weakness,
    string Impact,
    string StepsToReproduce,
    string ProofOfConcept,
    string? Notes,
    string? MarkdownBody,
    int ReportReadinessScore,
    HackerOneReportDraftStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? TurkishMarkdownBody = null);

public sealed record CreateHackerOneDraftRequest(Guid FindingId, Guid? BugBountyProgramId);

public sealed record UpdateHackerOneDraftRequest(
    string? Title,
    string? Severity,
    string? Asset,
    string? Weakness,
    string? Impact,
    string? StepsToReproduce,
    string? ProofOfConcept,
    string? Notes);

public sealed record HackerOneMarkdownDto(
    Guid DraftId,
    string Markdown,
    int ReportReadinessScore,
    string Language = "en-US",
    string? TurkishMarkdown = null);

public sealed record HackerOneSubmissionDto(
    Guid Id,
    Guid DraftId,
    string? ExternalReportId,
    string? ExternalReportUrl,
    HackerOneSubmissionStatus Status,
    string? ErrorMessage,
    DateTime? SubmittedAt);

public sealed record SubmitHackerOneDraftRequest(bool ExplicitConfirm);

public sealed record ScanProfileDto(
    Guid Id,
    string ProfileKey,
    string DisplayName,
    string UserAgentConfigKey,
    string RateLimitPerMinuteConfigKey,
    bool IsEnabled,
    string? ResolvedUserAgent,
    int ResolvedRateLimitPerMinute);

/// <summary>DomainAssetId veya HostName (örn. amazon.com) — en az biri zorunlu.</summary>
public sealed record StartCandidateAssessmentRequest(Guid? DomainAssetId = null, string? HostName = null);

/// <summary>Targets sayfasında “neler tarandı” paneli + güvenlik raporu indirme için.</summary>
public sealed record TargetAssessmentSummaryDto(
    Guid DomainAssetId,
    string HostName,
    Guid ScanJobId,
    Guid? ScanResultId,
    string Status,
    DateTime? CompletedAt,
    int SecurityScore,
    string? Summary,
    string? ExecutiveSummary,
    int ChecksTotal,
    int ChecksPassed,
    int ChecksFailed,
    int CriticalCount,
    int HighCount,
    int MediumCount,
    int LowCount,
    int InfoCount,
    IReadOnlyList<string> EnginesRun,
    IReadOnlyList<TargetAssessmentFindingDto> Findings);

public sealed record TargetAssessmentFindingDto(
    Guid FindingId,
    string Title,
    string Severity,
    string FindingClass,
    string SubmissionRecommendation,
    string? AffectedUrl,
    string? CheckCode,
    string? Fingerprint,
    string? Category);
