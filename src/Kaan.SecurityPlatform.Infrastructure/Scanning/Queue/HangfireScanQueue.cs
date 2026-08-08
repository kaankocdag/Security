using Hangfire;
using Kaan.SecurityPlatform.Application.Common.Interfaces;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Queue;

public sealed class HangfireScanQueue : IScanQueue
{
    private readonly IBackgroundJobClient _client;

    public HangfireScanQueue(IBackgroundJobClient client)
    {
        _client = client;
    }

    public Task<string> EnqueueScanAsync(Guid scanJobId, CancellationToken cancellationToken = default)
    {
        var id = _client.Enqueue<IScanExecutor>(x => x.ExecuteAsync(scanJobId, CancellationToken.None));
        return Task.FromResult(id);
    }

    public Task<string> ScheduleScanAsync(Guid scanJobId, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        var id = _client.Schedule<IScanExecutor>(x => x.ExecuteAsync(scanJobId, CancellationToken.None), delay);
        return Task.FromResult(id);
    }

    public Task<bool> CancelScheduledAsync(string queueId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_client.Delete(queueId));
    }
}
