namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

public interface IScanQueue
{
    Task<string> EnqueueScanAsync(Guid scanJobId, CancellationToken cancellationToken = default);
    Task<string> ScheduleScanAsync(Guid scanJobId, TimeSpan delay, CancellationToken cancellationToken = default);
    Task<bool> CancelScheduledAsync(string queueId, CancellationToken cancellationToken = default);
}
