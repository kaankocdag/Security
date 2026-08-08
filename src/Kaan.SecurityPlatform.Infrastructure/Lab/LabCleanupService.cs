using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;
using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Lab;

public sealed class LabCleanupService : ILabCleanupService
{
    private readonly IApplicationDbContext _db;
    private readonly ILabEnvironmentService _environments;
    private readonly ILabAuditService _audit;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<LabCleanupService> _logger;

    public LabCleanupService(
        IApplicationDbContext db,
        ILabEnvironmentService environments,
        ILabAuditService audit,
        IDateTimeProvider clock,
        ILogger<LabCleanupService> logger)
    {
        _db = db;
        _environments = environments;
        _audit = audit;
        _clock = clock;
        _logger = logger;
    }

    public async Task CleanupExecutionAsync(Guid executionId, string reasonTr, CancellationToken cancellationToken = default)
    {
        var execution = await _db.LabExecutions.FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);
        if (execution is null) return;

        var previous = execution.Status;
        if (previous is not (LabExecutionStatus.Cancelled or LabExecutionStatus.Failed or LabExecutionStatus.Completed))
        {
            execution.Status = LabExecutionStatus.CleaningUp;
            await _db.SaveChangesAsync(cancellationToken);
        }

        try
        {
            await _environments.StopAsync(executionId, cancellationToken);
            await _environments.DestroyAsync(executionId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lab cleanup sırasında hata {Id}", executionId);
        }

        execution = await _db.LabExecutions.FirstAsync(e => e.Id == executionId, cancellationToken);
        if (execution.Status is not (LabExecutionStatus.Cancelled or LabExecutionStatus.Failed or LabExecutionStatus.Completed))
        {
            execution.Status = LabExecutionStatus.Destroyed;
        }

        execution.CompletedAt ??= _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(execution.AuditCorrelationId, "lab.execution.cleanup", "LabExecution",
            execution.Id.ToString(), new { reasonTr }, cancellationToken);
    }

    public async Task SweepExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var expired = await _db.LabEnvironments
            .Where(e => e.ExpiresAt != null
                        && e.ExpiresAt < now
                        && e.Status != LabEnvironmentStatus.Destroyed)
            .Select(e => e.LabExecutionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var id in expired)
        {
            _logger.LogInformation("TTL dolmuş lab temizleniyor {Id}", id);
            await CleanupExecutionAsync(id, "TTL süresi doldu", cancellationToken);
        }
    }
}
