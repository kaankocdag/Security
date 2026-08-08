using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Features.Lab.Dtos;

public sealed record ElevateLabRequest(string Password);

public sealed record ElevateLabResponse(
    string ElevationToken,
    DateTime ExpiresAtUtc,
    int LifetimeMinutes);

public sealed record StartLabExecutionRequest(
    string ScenarioKey,
    string ConfirmPhrase,
    string ElevationToken,
    Guid LabTargetSiteId,
    string? AssessmentModeName = null);

public sealed record LabTargetSiteDto(
    Guid Id,
    string HostName,
    string NormalizedHostName,
    string? NotesTr,
    bool IsEnabled,
    DateTime CreatedAt);

public sealed record CreateLabTargetSiteRequest(
    string HostName,
    string? NotesTr);

public sealed record LabScenarioDto(
    string ScenarioKey,
    string TitleTr,
    string SummaryTr,
    LabRiskCategory RiskCategory,
    bool IsFullyImplemented,
    int DisplayOrder);

public sealed record LabExecutionListItemDto(
    Guid Id,
    string ScenarioKey,
    string ScenarioTitleTr,
    string TargetHostName,
    LabExecutionStatus Status,
    LabRuntimeMode RuntimeMode,
    Guid AuditCorrelationId,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public sealed record LabExecutionStepDto(
    LabStepKind StepKind,
    int StepOrder,
    string TitleTr,
    LabStepStatus Status,
    string? SummaryTr,
    DateTime? StartedAt,
    DateTime? CompletedAt);

public sealed record LabComparisonDto(
    bool InitialTestFailed,
    bool RetestSucceeded,
    int VulnerableScore,
    int PatchedScore,
    string RiskTr,
    string WhyTr,
    string FixTr,
    string SummaryTr);

public sealed record LabExecutionDetailDto(
    Guid Id,
    string ScenarioKey,
    string ScenarioTitleTr,
    string TargetHostName,
    AssessmentMode AssessmentMode,
    LabExecutionStatus Status,
    LabRuntimeMode RuntimeMode,
    Guid AuditCorrelationId,
    string ElevatedByEmail,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? FailureReasonTr,
    IReadOnlyList<LabExecutionStepDto> Steps,
    LabComparisonDto? Comparison);

public sealed record LabExecutionLogDto(
    Guid Id,
    string Level,
    string MessageTr,
    DateTime LoggedAt,
    Guid? LabExecutionStepId);

public sealed record StartLabExecutionResponse(
    Guid ExecutionId,
    Guid AuditCorrelationId,
    LabExecutionStatus Status);
