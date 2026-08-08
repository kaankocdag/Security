using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Executor;

/// <summary>AssessmentMode'a göre Passive veya ApplicationSecurityCandidate executor'a yönlendirir.</summary>
public sealed class RoutingScanExecutor : IScanExecutor
{
    private readonly SecurityPlatformDbContext _db;
    private readonly PassiveScanExecutor _passive;
    private readonly ApplicationSecurityCandidateExecutor _candidate;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<RoutingScanExecutor> _logger;

    public RoutingScanExecutor(
        SecurityPlatformDbContext db,
        PassiveScanExecutor passive,
        ApplicationSecurityCandidateExecutor candidate,
        IDateTimeProvider clock,
        ILogger<RoutingScanExecutor> logger)
    {
        _db = db;
        _passive = passive;
        _candidate = candidate;
        _clock = clock;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid scanJobId, CancellationToken cancellationToken = default)
    {
        var mode = await _db.ScanJobs.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(j => j.Id == scanJobId)
            .Select(j => (AssessmentMode?)j.AssessmentMode)
            .FirstOrDefaultAsync(cancellationToken);

        if (mode is null)
        {
            _logger.LogWarning("Tarama işi bulunamadı: {ScanJobId}", scanJobId);
            return;
        }

        if (mode == AssessmentMode.ApplicationSecurityCandidate)
        {
            var now = _clock.UtcNow;
            var staleBefore = now.AddMinutes(-5);
            var claimed = await _db.ScanJobs
                .IgnoreQueryFilters()
                .Where(j => j.Id == scanJobId && (
                    j.Status == ScanStatus.Queued ||
                    j.Status == ScanStatus.Failed ||
                    (j.Status == ScanStatus.Running && j.StartedAt != null && j.StartedAt < staleBefore)))
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(j => j.Status, ScanStatus.Running)
                        .SetProperty(j => j.StartedAt, now)
                        .SetProperty(j => j.CompletedAt, (DateTime?)null)
                        .SetProperty(j => j.ProgressPercentage, 0)
                        .SetProperty(j => j.CompletedSteps, 0)
                        .SetProperty(j => j.CurrentStep, (string?)null)
                        .SetProperty(j => j.ErrorMessage, (string?)null),
                    cancellationToken);

            if (claimed == 0)
            {
                _logger.LogInformation("Candidate tarama atlandı: {ScanJobId}", scanJobId);
                return;
            }

            await _candidate.ExecuteAsync(scanJobId, cancellationToken);
            return;
        }

        await _passive.ExecuteAsync(scanJobId, cancellationToken);
    }
}
