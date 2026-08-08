namespace Kaan.SecurityPlatform.LabWorker;

public sealed class LabWorkerHealthLog : BackgroundService
{
    private readonly ILogger<LabWorkerHealthLog> _logger;

    public LabWorkerHealthLog(ILogger<LabWorkerHealthLog> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LabWorker sağlık günlüğü aktif (kuyruk: labs)");
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            _logger.LogDebug("LabWorker ayakta");
        }
    }
}
