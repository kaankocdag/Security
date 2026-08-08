using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.ScannerWorker;

public sealed class ScannerWorkerHealthLog : BackgroundService
{
    private readonly ILogger<ScannerWorkerHealthLog> _logger;

    public ScannerWorkerHealthLog(ILogger<ScannerWorkerHealthLog> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("ScannerWorker heartbeat: {Time}", DateTime.UtcNow);
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
