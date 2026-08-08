using Hangfire;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;
using Kaan.SecurityPlatform.Application.Features.Lab;
using Kaan.SecurityPlatform.Domain.Entities.Lab;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Lab.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Lab;

public sealed class LabExecutionRunner : ILabExecutionRunner
{
    private readonly IApplicationDbContext _db;
    private readonly ILabScenarioRegistry _registry;
    private readonly ILabEnvironmentService _environments;
    private readonly ILabAuditService _audit;
    private readonly ILabCleanupService _cleanup;
    private readonly IDateTimeProvider _clock;
    private readonly IServiceProvider _services;
    private readonly ILogger<LabExecutionRunner> _logger;

    public LabExecutionRunner(
        IApplicationDbContext db,
        ILabScenarioRegistry registry,
        ILabEnvironmentService environments,
        ILabAuditService audit,
        ILabCleanupService cleanup,
        IDateTimeProvider clock,
        IServiceProvider services,
        ILogger<LabExecutionRunner> logger)
    {
        _db = db;
        _registry = registry;
        _environments = environments;
        _audit = audit;
        _cleanup = cleanup;
        _clock = clock;
        _services = services;
        _logger = logger;
    }

    [Queue(LabConstants.HangfireQueue)]
    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var execution = await _db.LabExecutions
            .Include(e => e.Steps)
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        if (execution is null)
        {
            _logger.LogWarning("Lab execution bulunamadı {Id}", executionId);
            return;
        }

        if (execution.Status is LabExecutionStatus.Cancelled or LabExecutionStatus.Destroyed)
        {
            return;
        }

        var scenario = _registry.Get(execution.ScenarioKey);
        if (scenario is null)
        {
            execution.Status = LabExecutionStatus.Failed;
            execution.FailureReasonTr = "Senaryo kayıtlı değil.";
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        execution.Status = LabExecutionStatus.Running;
        execution.StartedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        LabProbeResult? vulnerableProbe = null;
        LabProbeResult? patchedProbe = null;

        try
        {
            var handle = await _environments.CreateAsync(executionId, scenario.ScenarioKey, cancellationToken);
            execution.RuntimeMode = handle.RuntimeMode;
            await _db.SaveChangesAsync(cancellationToken);

            var runtime = handle.RuntimeMode == LabRuntimeMode.Docker
                ? (ILabRuntime)_services.GetRequiredService<DockerLabRuntime>()
                : _services.GetRequiredService<MockLabRuntime>();

            foreach (var step in execution.Steps.OrderBy(s => s.StepOrder))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await RefreshCancelCheck(execution, cancellationToken);
                if (execution.Status == LabExecutionStatus.Cancelled)
                {
                    await _cleanup.CleanupExecutionAsync(executionId, "İptal edildi", cancellationToken);
                    return;
                }

                step.Status = LabStepStatus.Running;
                step.StartedAt = _clock.UtcNow;
                await AddLog(execution, step.Id, "Info", $"Adım başladı: {step.TitleTr}", cancellationToken);
                await _db.SaveChangesAsync(cancellationToken);

                try
                {
                    switch (step.StepKind)
                    {
                        case LabStepKind.VulnerableStart:
                            await _environments.StartVulnerableAsync(executionId, cancellationToken);
                            step.SummaryTr = "Zayıf lab ortamı hazır.";
                            break;
                        case LabStepKind.ControlRun:
                            vulnerableProbe = await runtime.ProbeAsync(executionId, patched: false, cancellationToken);
                            step.SummaryTr = vulnerableProbe.SummaryTr;
                            break;
                        case LabStepKind.ImpactDemo:
                            step.SummaryTr = scenario.GetSignedPlan().Comparison.RiskTr;
                            break;
                        case LabStepKind.ShowLogs:
                            step.SummaryTr = "Sanitize loglar kaydedildi (payload yok).";
                            break;
                        case LabStepKind.ExplainSecure:
                            step.SummaryTr = scenario.GetSignedPlan().Comparison.FixTr;
                            break;
                        case LabStepKind.ShowPatch:
                            step.SummaryTr = $"Patched imaj: {scenario.PatchedImageTag}";
                            break;
                        case LabStepKind.SecureStart:
                            await _environments.StartPatchedAsync(executionId, cancellationToken);
                            step.SummaryTr = "Güvenli lab ortamı hazır.";
                            break;
                        case LabStepKind.Retest:
                            patchedProbe = await runtime.ProbeAsync(executionId, patched: true, cancellationToken);
                            step.SummaryTr = patchedProbe.SummaryTr;
                            break;
                        case LabStepKind.Compare:
                            await WriteComparison(execution, scenario, vulnerableProbe, patchedProbe, cancellationToken);
                            step.SummaryTr = "Karşılaştırma tamamlandı.";
                            break;
                        case LabStepKind.Destroy:
                            await _environments.DestroyAsync(executionId, cancellationToken);
                            step.SummaryTr = "Lab ortamı yok edildi.";
                            break;
                    }

                    step.Status = LabStepStatus.Succeeded;
                    step.CompletedAt = _clock.UtcNow;
                    await AddLog(execution, step.Id, "Info", step.SummaryTr ?? "Adım tamamlandı.", cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lab adımı başarısız {ExecutionId} {Step}", executionId, step.StepKind);
                    step.Status = LabStepStatus.Failed;
                    step.SummaryTr = "Adım başarısız; ortam temizlenecek.";
                    step.CompletedAt = _clock.UtcNow;
                    execution.Status = LabExecutionStatus.Failed;
                    execution.FailureReasonTr = "Laboratuvar adımı başarısız oldu.";
                    await _db.SaveChangesAsync(cancellationToken);
                    await _cleanup.CleanupExecutionAsync(executionId, "Adım hatası", cancellationToken);
                    await _audit.WriteAsync(execution.AuditCorrelationId, "lab.execution.failed", "LabExecution",
                        execution.Id.ToString(), new { step.StepKind }, cancellationToken);
                    return;
                }

                await _db.SaveChangesAsync(cancellationToken);
            }

            execution.Status = LabExecutionStatus.Completed;
            execution.CompletedAt = _clock.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            await _audit.WriteAsync(execution.AuditCorrelationId, "lab.execution.completed", "LabExecution",
                execution.Id.ToString(), null, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            execution.Status = LabExecutionStatus.Cancelled;
            execution.CancelledAt = _clock.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            await _cleanup.CleanupExecutionAsync(executionId, "İptal", CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lab execution başarısız {Id}", executionId);
            execution.Status = LabExecutionStatus.Failed;
            execution.FailureReasonTr = "Laboratuvar çalıştırma hatası.";
            await _db.SaveChangesAsync(cancellationToken);
            await _cleanup.CleanupExecutionAsync(executionId, "Çalıştırma hatası", CancellationToken.None);
        }
    }

    private async Task RefreshCancelCheck(LabExecution execution, CancellationToken cancellationToken)
    {
        var status = await _db.LabExecutions.AsNoTracking()
            .Where(e => e.Id == execution.Id)
            .Select(e => e.Status)
            .FirstAsync(cancellationToken);
        if (status == LabExecutionStatus.Cancelled)
        {
            execution.Status = LabExecutionStatus.Cancelled;
        }
    }

    private async Task WriteComparison(
        LabExecution execution,
        ILabScenario scenario,
        LabProbeResult? vulnerable,
        LabProbeResult? patched,
        CancellationToken cancellationToken)
    {
        var template = scenario.GetSignedPlan().Comparison;
        var cmp = await _db.LabComparisonResults.FirstOrDefaultAsync(c => c.LabExecutionId == execution.Id, cancellationToken);
        if (cmp is null)
        {
            cmp = new LabComparisonResult { LabExecutionId = execution.Id };
            _db.LabComparisonResults.Add(cmp);
        }

        cmp.InitialTestFailed = vulnerable?.ControlFailed ?? true;
        cmp.RetestSucceeded = patched is { ControlFailed: false };
        cmp.VulnerableScore = vulnerable?.Score ?? 0;
        cmp.PatchedScore = patched?.Score ?? 0;
        cmp.RiskTr = template.RiskTr;
        cmp.WhyTr = template.WhyTr;
        cmp.FixTr = template.FixTr;
        cmp.SummaryTr = template.SummaryTr;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task AddLog(
        LabExecution execution,
        Guid? stepId,
        string level,
        string messageTr,
        CancellationToken cancellationToken)
    {
        _db.LabExecutionLogs.Add(new LabExecutionLog
        {
            LabExecutionId = execution.Id,
            LabExecutionStepId = stepId,
            Level = level,
            MessageTr = messageTr.Length > 2000 ? messageTr[..2000] : messageTr,
            LoggedAt = _clock.UtcNow
        });
        await Task.CompletedTask;
    }
}
