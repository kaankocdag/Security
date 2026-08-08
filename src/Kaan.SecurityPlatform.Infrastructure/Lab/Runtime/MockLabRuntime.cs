using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;
using Kaan.SecurityPlatform.Application.Features.Lab;
using Kaan.SecurityPlatform.Domain.Entities.Lab;
using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Kaan.SecurityPlatform.Infrastructure.Lab.Runtime;

public sealed class MockLabRuntime : ILabRuntime
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly LabOptions _options;

    public MockLabRuntime(
        IApplicationDbContext db,
        IDateTimeProvider clock,
        IOptions<LabOptions> options)
    {
        _db = db;
        _clock = clock;
        _options = options.Value;
    }

    public LabRuntimeMode Mode => LabRuntimeMode.Mock;

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public async Task<LabRuntimeHandle> CreateEnvironmentAsync(
        Guid executionId,
        string scenarioKey,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var endpoint = $"http://lab-mock-{executionId:N}.kaan-lab.internal:8080";
        var env = await _db.LabEnvironments.FirstOrDefaultAsync(e => e.LabExecutionId == executionId, cancellationToken);
        if (env is null)
        {
            env = new LabEnvironment
            {
                LabExecutionId = executionId,
                RuntimeMode = LabRuntimeMode.Mock,
                Status = LabEnvironmentStatus.Ready,
                NetworkName = LabConstants.InternalNetworkName,
                NetworkId = $"mock-net-{executionId:N}"[..12],
                InternalEndpoint = endpoint,
                StartedAt = now,
                ExpiresAt = now.AddMinutes(_options.ExecutionTimeoutMinutes)
            };
            _db.LabEnvironments.Add(env);
        }
        else
        {
            env.RuntimeMode = LabRuntimeMode.Mock;
            env.Status = LabEnvironmentStatus.Ready;
            env.InternalEndpoint = endpoint;
            env.NetworkName = LabConstants.InternalNetworkName;
            env.StartedAt = now;
            env.ExpiresAt = now.AddMinutes(_options.ExecutionTimeoutMinutes);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new LabRuntimeHandle(env.Id, LabRuntimeMode.Mock, endpoint);
    }

    public async Task StartVulnerableAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var env = await RequireEnv(executionId, cancellationToken);
        env.Status = LabEnvironmentStatus.RunningVulnerable;
        env.VulnerableContainerId = $"mock-vuln-{executionId:N}"[..16];
        await _db.SaveChangesAsync(cancellationToken);
        await Task.Delay(150, cancellationToken);
    }

    public async Task StartPatchedAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var env = await RequireEnv(executionId, cancellationToken);
        env.Status = LabEnvironmentStatus.RunningPatched;
        env.PatchedContainerId = $"mock-patch-{executionId:N}"[..16];
        await _db.SaveChangesAsync(cancellationToken);
        await Task.Delay(150, cancellationToken);
    }

    public async Task StopAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var env = await _db.LabEnvironments.FirstOrDefaultAsync(e => e.LabExecutionId == executionId, cancellationToken);
        if (env is null) return;
        env.Status = LabEnvironmentStatus.Stopping;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DestroyAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var env = await _db.LabEnvironments.FirstOrDefaultAsync(e => e.LabExecutionId == executionId, cancellationToken);
        if (env is null) return;
        env.Status = LabEnvironmentStatus.Destroyed;
        env.DestroyedAt = _clock.UtcNow;
        env.VulnerableContainerId = null;
        env.PatchedContainerId = null;
        env.InternalEndpoint = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<LabProbeResult> ProbeAsync(Guid executionId, bool patched, CancellationToken cancellationToken = default)
    {
        if (patched)
        {
            return Task.FromResult(new LabProbeResult(
                ControlFailed: false,
                Score: 95,
                SummaryTr: "Yeniden test: imzalı kontrol güvenli lab ortamında başarılı."));
        }

        return Task.FromResult(new LabProbeResult(
            ControlFailed: true,
            Score: 35,
            SummaryTr: "İlk test: imzalı kontrol zayıf lab ortamında başarısız (eğitim simülasyonu)."));
    }

    private async Task<LabEnvironment> RequireEnv(Guid executionId, CancellationToken cancellationToken)
    {
        var env = await _db.LabEnvironments.FirstOrDefaultAsync(e => e.LabExecutionId == executionId, cancellationToken);
        if (env is null)
        {
            throw new InvalidOperationException($"Lab ortamı bulunamadı: {executionId}");
        }

        return env;
    }
}
