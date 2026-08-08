using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Lab;

public sealed class LabCleanupHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LabCleanupHostedService> _logger;

    public LabCleanupHostedService(IServiceScopeFactory scopeFactory, ILogger<LabCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cleanup = scope.ServiceProvider.GetRequiredService<ILabCleanupService>();
                await cleanup.SweepExpiredAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Lab TTL sweep hatası");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
