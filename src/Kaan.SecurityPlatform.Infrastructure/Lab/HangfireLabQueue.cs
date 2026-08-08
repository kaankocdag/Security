using Hangfire;
using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Lab;

public sealed class HangfireLabQueue : ILabQueue
{
    private readonly IBackgroundJobClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HangfireLabQueue> _logger;

    public HangfireLabQueue(
        IBackgroundJobClient client,
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        IConfiguration configuration,
        ILogger<HangfireLabQueue> logger)
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<string> EnqueueAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var id = _client.Enqueue<ILabExecutionRunner>(x => x.ExecuteAsync(executionId, CancellationToken.None));

        // MemoryStorage süreçler arası paylaşılmaz; Development'ta LabWorker yoksa Mock pipeline'ı yerelde çalıştır.
        var hangfireConn = _configuration.GetConnectionString("Hangfire");
        if (_environment.IsDevelopment() && string.IsNullOrWhiteSpace(hangfireConn))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(250);
                    using var scope = _scopeFactory.CreateScope();
                    var runner = scope.ServiceProvider.GetRequiredService<ILabExecutionRunner>();
                    await runner.ExecuteAsync(executionId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Development lab fallback çalıştırma hatası {ExecutionId}", executionId);
                }
            }, CancellationToken.None);
        }

        return Task.FromResult(id);
    }
}
