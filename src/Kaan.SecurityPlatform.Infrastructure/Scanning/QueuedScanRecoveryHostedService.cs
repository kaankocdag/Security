using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning;

/// <summary>
/// Development'ta MemoryStorage nedeniyle kaybolan Queued işleri
/// ve takılı kalan Running taramaları yeniden kuyruğa alır.
/// </summary>
public sealed class QueuedScanRecoveryHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<QueuedScanRecoveryHostedService> _logger;

    public QueuedScanRecoveryHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<QueuedScanRecoveryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(4), stoppingToken);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var queue = scope.ServiceProvider.GetRequiredService<IScanQueue>();
            var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
            var staleBefore = clock.UtcNow.AddMinutes(-5);

            var stuck = await db.ScanJobs
                .Where(j =>
                    j.Status == ScanStatus.Queued ||
                    (j.Status == ScanStatus.Running && j.StartedAt != null && j.StartedAt < staleBefore))
                .OrderBy(j => j.CreatedAt)
                .Take(20)
                .Select(j => new { j.Id, j.Status })
                .ToListAsync(stoppingToken);

            foreach (var item in stuck)
            {
                if (item.Status == ScanStatus.Running)
                {
                    // Executor atomik claim ile tekrar alabilsin
                    await db.ScanJobs
                        .Where(j => j.Id == item.Id && j.Status == ScanStatus.Running)
                        .ExecuteUpdateAsync(
                            s => s.SetProperty(j => j.Status, ScanStatus.Queued),
                            stoppingToken);
                }

                _logger.LogInformation("Queued tarama yeniden kuyruğa alınıyor: {ScanJobId}", item.Id);
                await queue.EnqueueScanAsync(item.Id, stoppingToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Queued tarama kurtarma başarısız");
        }
    }
}
