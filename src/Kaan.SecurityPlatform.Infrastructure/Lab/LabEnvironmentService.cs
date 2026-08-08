using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;
using Kaan.SecurityPlatform.Infrastructure.Lab.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kaan.SecurityPlatform.Infrastructure.Lab;

public sealed class LabEnvironmentService : ILabEnvironmentService
{
    private readonly IServiceProvider _services;
    private readonly IApplicationDbContext _db;
    private readonly LabOptions _options;
    private readonly ILogger<LabEnvironmentService> _logger;
    private ILabRuntime? _active;

    public LabEnvironmentService(
        IServiceProvider services,
        IApplicationDbContext db,
        IOptions<LabOptions> options,
        ILogger<LabEnvironmentService> logger)
    {
        _services = services;
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<LabRuntimeHandle> CreateAsync(Guid executionId, string scenarioKey, CancellationToken cancellationToken = default)
    {
        var runtime = await ResolveRuntimeAsync(cancellationToken);
        _active = runtime;
        return await runtime.CreateEnvironmentAsync(executionId, scenarioKey, cancellationToken);
    }

    public async Task StartVulnerableAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var runtime = await EnsureRuntimeAsync(cancellationToken);
        await runtime.StartVulnerableAsync(executionId, cancellationToken);
    }

    public async Task StartPatchedAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var runtime = await EnsureRuntimeAsync(cancellationToken);
        await runtime.StartPatchedAsync(executionId, cancellationToken);
    }

    public async Task StopAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var runtime = await EnsureRuntimeAsync(cancellationToken);
        await runtime.StopAsync(executionId, cancellationToken);
    }

    public async Task DestroyAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var runtime = await EnsureRuntimeAsync(cancellationToken);
        await runtime.DestroyAsync(executionId, cancellationToken);
    }

    public async Task<string?> GetInternalEndpointAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var env = await _db.LabEnvironments.AsNoTracking()
            .FirstOrDefaultAsync(e => e.LabExecutionId == executionId, cancellationToken);
        return env?.InternalEndpoint;
    }

    private async Task<ILabRuntime> EnsureRuntimeAsync(CancellationToken cancellationToken)
    {
        if (_active is not null)
        {
            return _active;
        }

        _active = await ResolveRuntimeAsync(cancellationToken);
        return _active;
    }

    private async Task<ILabRuntime> ResolveRuntimeAsync(CancellationToken cancellationToken)
    {
        var mode = _options.RuntimeMode?.Trim() ?? "Auto";
        var docker = _services.GetRequiredService<DockerLabRuntime>();
        var mock = _services.GetRequiredService<MockLabRuntime>();

        if (string.Equals(mode, "Mock", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Lab runtime: Mock (yapılandırma)");
            return mock;
        }

        if (string.Equals(mode, "Docker", StringComparison.OrdinalIgnoreCase))
        {
            if (!await docker.IsAvailableAsync(cancellationToken))
            {
                throw new InvalidOperationException("Lab RuntimeMode=Docker ancak Docker erişilemiyor.");
            }

            _logger.LogInformation("Lab runtime: Docker (yapılandırma)");
            return docker;
        }

        if (await docker.IsAvailableAsync(cancellationToken))
        {
            _logger.LogInformation("Lab runtime: Docker (otomatik)");
            return docker;
        }

        _logger.LogInformation("Lab runtime: Mock (Docker yok)");
        return mock;
    }
}
