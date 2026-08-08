using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Features.Scans.Dtos;

public sealed record StartScanRequest(
    Guid DomainAssetId,
    ScanType ScanType = ScanType.FullPassive,
    AssessmentMode AssessmentMode = AssessmentMode.PublicPassiveAssessment,
    string? AssessmentModeName = null);

public sealed record StartScanResponse(
    Guid ScanJobId,
    ScanStatus Status,
    string? QueueId);

public sealed record ScanJobListItemDto(
    Guid Id,
    Guid SecurityProjectId,
    Guid DomainAssetId,
    string DomainHostName,
    ScanType ScanType,
    AssessmentMode AssessmentMode,
    ScanStatus Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int ProgressPercentage,
    string? CurrentStep,
    int? Score);

public sealed record ScanJobDetailDto(
    Guid Id,
    Guid SecurityProjectId,
    Guid DomainAssetId,
    string DomainHostName,
    ScanType ScanType,
    AssessmentMode AssessmentMode,
    ScanStatus Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int ProgressPercentage,
    string? CurrentStep,
    int TotalSteps,
    int CompletedSteps,
    string? ErrorMessage,
    bool IsRetest,
    ScanResultDto? Result);

public sealed record ScanResultDto(
    Guid Id,
    int SecurityScore,
    int PreviousSecurityScore,
    int CriticalCount,
    int HighCount,
    int MediumCount,
    int LowCount,
    int InfoCount,
    string? ExecutiveSummary,
    string? Summary,
    int ChecksTotal,
    int ChecksPassed,
    int ChecksFailed);

public sealed record ScanProgressDto(
    Guid ScanJobId,
    ScanStatus Status,
    int ProgressPercentage,
    string? CurrentStep,
    int CompletedSteps,
    int TotalSteps);

public sealed record RetestRequest(
    Guid FindingId);
