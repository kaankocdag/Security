using Hangfire;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.HackerOne;

public sealed class HackerOneScopeSyncJob : IHackerOneScopeSyncJob
{
    private readonly IHackerOneWorkspaceService _workspace;
    private readonly ILogger<HackerOneScopeSyncJob> _logger;

    public HackerOneScopeSyncJob(IHackerOneWorkspaceService workspace, ILogger<HackerOneScopeSyncJob> logger)
    {
        _workspace = workspace;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(timeoutInSeconds: 60 * 60 * 6)]
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HackerOne scope → domains sync started");
        var result = await _workspace.SyncScopesToDomainsAsync(cancellationToken);
        if (result.IsFailure)
        {
            // Auth/API hatalarında exception fırlatma — Hangfire + VS debugger'ı "unhandled" diye kesmesin.
            _logger.LogError(
                "HackerOne scope sync failed: {Code} {Message}",
                result.ErrorCode,
                result.ErrorMessage);
            return;
        }

        _logger.LogInformation("HackerOne scope sync finished: {Message}", result.Value!.Message);
    }
}
