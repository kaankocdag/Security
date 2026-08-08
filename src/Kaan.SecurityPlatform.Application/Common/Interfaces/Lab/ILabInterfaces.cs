using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Lab.Dtos;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;

public interface ILabScenario
{
    string ScenarioKey { get; }
    string TitleTr { get; }
    string SummaryTr { get; }
    LabRiskCategory RiskCategory { get; }
    string VulnerableImageTag { get; }
    string PatchedImageTag { get; }
    bool IsFullyImplemented { get; }
    int DisplayOrder { get; }
    LabSignedPlan GetSignedPlan();
}

public sealed record LabSignedPlan(
    string ScenarioKey,
    IReadOnlyList<LabSignedStep> Steps,
    LabComparisonTemplate Comparison);

public sealed record LabSignedStep(
    LabStepKind StepKind,
    int StepOrder,
    string TitleTr,
    string ExpectedOutcomeTr);

public sealed record LabComparisonTemplate(
    string RiskTr,
    string WhyTr,
    string FixTr,
    string SummaryTr);

public interface ILabScenarioRegistry
{
    IReadOnlyList<ILabScenario> GetAll();
    ILabScenario? Get(string scenarioKey);
    bool IsRegistered(string scenarioKey);
}

public interface ILabExecutionService
{
    Task<Result<ElevateLabResponse>> ElevateAsync(ElevateLabRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabScenarioDto>> ListScenariosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabTargetSiteDto>> ListTargetSitesAsync(CancellationToken cancellationToken = default);
    Task<Result<LabTargetSiteDto>> AddTargetSiteAsync(CreateLabTargetSiteRequest request, CancellationToken cancellationToken = default);
    Task<Result> DisableTargetSiteAsync(Guid targetSiteId, CancellationToken cancellationToken = default);
    Task<Result<StartLabExecutionResponse>> StartAsync(StartLabExecutionRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabExecutionListItemDto>> ListExecutionsAsync(CancellationToken cancellationToken = default);
    Task<Result<LabExecutionDetailDto>> GetAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<LabExecutionLogDto>>> GetLogsAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<Result> CancelAsync(Guid executionId, string? reasonTr = null, CancellationToken cancellationToken = default);
}

public interface ILabEnvironmentService
{
    Task<LabRuntimeHandle> CreateAsync(Guid executionId, string scenarioKey, CancellationToken cancellationToken = default);
    Task StartVulnerableAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task StartPatchedAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task StopAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task DestroyAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<string?> GetInternalEndpointAsync(Guid executionId, CancellationToken cancellationToken = default);
}

public sealed record LabRuntimeHandle(
    Guid EnvironmentId,
    LabRuntimeMode RuntimeMode,
    string? InternalEndpoint);

public interface ILabRuntime
{
    LabRuntimeMode Mode { get; }
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<LabRuntimeHandle> CreateEnvironmentAsync(Guid executionId, string scenarioKey, CancellationToken cancellationToken = default);
    Task StartVulnerableAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task StartPatchedAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task StopAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task DestroyAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task<LabProbeResult> ProbeAsync(Guid executionId, bool patched, CancellationToken cancellationToken = default);
}

public sealed record LabProbeResult(
    bool ControlFailed,
    int Score,
    string SummaryTr);

public interface ILabNetworkPolicyValidator
{
    Result ValidateTarget(Guid executionId, string? requestedTarget, string? allowedInternalEndpoint, string? allowedExternalHost = null);
}

public interface ILabAuditService
{
    Task WriteAsync(
        Guid correlationId,
        string action,
        string entityType,
        string? entityId = null,
        object? details = null,
        CancellationToken cancellationToken = default);
}

public interface ILabCleanupService
{
    Task CleanupExecutionAsync(Guid executionId, string reasonTr, CancellationToken cancellationToken = default);
    Task SweepExpiredAsync(CancellationToken cancellationToken = default);
}

public interface ILabExecutionRunner
{
    Task ExecuteAsync(Guid executionId, CancellationToken cancellationToken = default);
}

public interface ILabQueue
{
    Task<string> EnqueueAsync(Guid executionId, CancellationToken cancellationToken = default);
}

public interface ILabStartRequestGuard
{
    Result ValidateNoForbiddenFields(IDictionary<string, object?> rawFields);
}
