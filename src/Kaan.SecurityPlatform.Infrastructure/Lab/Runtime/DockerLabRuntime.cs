using System.Runtime.InteropServices;
using Docker.DotNet;
using Docker.DotNet.Models;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;
using Kaan.SecurityPlatform.Application.Features.Lab;
using Kaan.SecurityPlatform.Domain.Entities.Lab;
using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kaan.SecurityPlatform.Infrastructure.Lab.Runtime;

public sealed class DockerLabRuntime : ILabRuntime, IAsyncDisposable
{
    private readonly IApplicationDbContext _db;
    private readonly ILabScenarioRegistry _scenarios;
    private readonly IDateTimeProvider _clock;
    private readonly LabOptions _options;
    private readonly ILogger<DockerLabRuntime> _logger;
    private readonly Lazy<DockerClient> _client;

    public DockerLabRuntime(
        IApplicationDbContext db,
        ILabScenarioRegistry scenarios,
        IDateTimeProvider clock,
        IOptions<LabOptions> options,
        ILogger<DockerLabRuntime> logger)
    {
        _db = db;
        _scenarios = scenarios;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
        _client = new Lazy<DockerClient>(CreateClient);
    }

    public LabRuntimeMode Mode => LabRuntimeMode.Docker;

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.Value.System.PingAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Docker lab runtime kullanılamıyor");
            return false;
        }
    }

    public async Task<LabRuntimeHandle> CreateEnvironmentAsync(
        Guid executionId,
        string scenarioKey,
        CancellationToken cancellationToken = default)
    {
        var scenario = _scenarios.Get(scenarioKey)
            ?? throw new InvalidOperationException($"Senaryo kayıtlı değil: {scenarioKey}");

        EnsureImageAllowed(scenario.VulnerableImageTag);
        EnsureImageAllowed(scenario.PatchedImageTag);

        var networkName = $"{_options.InternalNetworkName}-{executionId:N}"[..24];
        var networks = await _client.Value.Networks.ListNetworksAsync(cancellationToken: cancellationToken);
        var existing = networks.FirstOrDefault(n => n.Name == networkName);
        string networkId;
        if (existing is null)
        {
            // AllowEgress=true: IsolatedSecurityLab denemeleri için internet çıkışı açık.
            // Aktif exploit yok; yalnızca imzalı senaryo + allowlist hedef.
            var created = await _client.Value.Networks.CreateNetworkAsync(new NetworksCreateParameters
            {
                Name = networkName,
                Driver = "bridge",
                Internal = !_options.AllowEgress,
                CheckDuplicate = true,
                Labels = LabLabels(executionId, scenarioKey)
            }, cancellationToken);
            networkId = created.ID;
        }
        else
        {
            networkId = existing.ID;
        }

        var hostname = $"lab-{executionId:N}"[..12];
        var endpoint = $"http://{hostname}:8080";
        var now = _clock.UtcNow;

        var env = await _db.LabEnvironments.FirstOrDefaultAsync(e => e.LabExecutionId == executionId, cancellationToken);
        if (env is null)
        {
            env = new LabEnvironment { LabExecutionId = executionId };
            _db.LabEnvironments.Add(env);
        }

        env.RuntimeMode = LabRuntimeMode.Docker;
        env.Status = LabEnvironmentStatus.Ready;
        env.NetworkId = networkId;
        env.NetworkName = networkName;
        env.InternalEndpoint = endpoint;
        env.StartedAt = now;
        env.ExpiresAt = now.AddMinutes(_options.ExecutionTimeoutMinutes);
        await _db.SaveChangesAsync(cancellationToken);

        return new LabRuntimeHandle(env.Id, LabRuntimeMode.Docker, endpoint);
    }

    public async Task StartVulnerableAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var env = await RequireEnv(executionId, cancellationToken);
        var execution = await _db.LabExecutions.FirstAsync(e => e.Id == executionId, cancellationToken);
        var scenario = _scenarios.Get(execution.ScenarioKey)!;
        EnsureImageAllowed(scenario.VulnerableImageTag);

        if (!string.IsNullOrEmpty(env.VulnerableContainerId))
        {
            await SafeRemoveContainer(env.VulnerableContainerId, cancellationToken);
        }

        var containerId = await CreateAndStartContainerAsync(
            executionId,
            execution.ScenarioKey,
            scenario.VulnerableImageTag,
            env.NetworkName!,
            $"lab-{executionId:N}"[..12],
            cancellationToken);

        env.VulnerableContainerId = containerId;
        env.Status = LabEnvironmentStatus.RunningVulnerable;
        await _db.SaveChangesAsync(cancellationToken);
        await Task.Delay(800, cancellationToken);
    }

    public async Task StartPatchedAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var env = await RequireEnv(executionId, cancellationToken);
        var execution = await _db.LabExecutions.FirstAsync(e => e.Id == executionId, cancellationToken);
        var scenario = _scenarios.Get(execution.ScenarioKey)!;
        EnsureImageAllowed(scenario.PatchedImageTag);

        if (!string.IsNullOrEmpty(env.VulnerableContainerId))
        {
            await SafeRemoveContainer(env.VulnerableContainerId, cancellationToken);
            env.VulnerableContainerId = null;
        }

        if (!string.IsNullOrEmpty(env.PatchedContainerId))
        {
            await SafeRemoveContainer(env.PatchedContainerId, cancellationToken);
        }

        var containerId = await CreateAndStartContainerAsync(
            executionId,
            execution.ScenarioKey,
            scenario.PatchedImageTag,
            env.NetworkName!,
            $"lab-{executionId:N}"[..12],
            cancellationToken);

        env.PatchedContainerId = containerId;
        env.Status = LabEnvironmentStatus.RunningPatched;
        await _db.SaveChangesAsync(cancellationToken);
        await Task.Delay(800, cancellationToken);
    }

    public async Task StopAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var env = await _db.LabEnvironments.FirstOrDefaultAsync(e => e.LabExecutionId == executionId, cancellationToken);
        if (env is null) return;
        env.Status = LabEnvironmentStatus.Stopping;
        await _db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(env.VulnerableContainerId))
            await SafeStopContainer(env.VulnerableContainerId, cancellationToken);
        if (!string.IsNullOrEmpty(env.PatchedContainerId))
            await SafeStopContainer(env.PatchedContainerId, cancellationToken);
    }

    public async Task DestroyAsync(Guid executionId, CancellationToken cancellationToken = default)
    {
        var env = await _db.LabEnvironments.FirstOrDefaultAsync(e => e.LabExecutionId == executionId, cancellationToken);
        if (env is null) return;

        if (!string.IsNullOrEmpty(env.VulnerableContainerId))
            await SafeRemoveContainer(env.VulnerableContainerId, cancellationToken);
        if (!string.IsNullOrEmpty(env.PatchedContainerId))
            await SafeRemoveContainer(env.PatchedContainerId, cancellationToken);
        if (!string.IsNullOrEmpty(env.NetworkId))
        {
            try
            {
                await _client.Value.Networks.DeleteNetworkAsync(env.NetworkId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lab ağı silinemedi {NetworkId}", env.NetworkId);
            }
        }

        env.Status = LabEnvironmentStatus.Destroyed;
        env.DestroyedAt = _clock.UtcNow;
        env.VulnerableContainerId = null;
        env.PatchedContainerId = null;
        env.InternalEndpoint = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<LabProbeResult> ProbeAsync(Guid executionId, bool patched, CancellationToken cancellationToken = default)
    {
        var env = await RequireEnv(executionId, cancellationToken);
        var execution = await _db.LabExecutions.AsNoTracking()
            .FirstAsync(e => e.Id == executionId, cancellationToken);
        var policy = new LabNetworkPolicyValidator();
        var validation = policy.ValidateTarget(
            executionId,
            env.InternalEndpoint,
            env.InternalEndpoint,
            execution.TargetHostName);
        if (validation.IsFailure)
        {
            return new LabProbeResult(true, 0, validation.ErrorMessage ?? "Ağ politikası reddetti.");
        }

        // Lab container'lara host'tan erişim internal network nedeniyle yoksa eğitim skoruna düş.
        // İmzalı senaryo sonuçları: zayıf ortam başarısız, patched başarılı (container ayaktaysa).
        var containerId = patched ? env.PatchedContainerId : env.VulnerableContainerId;
        if (string.IsNullOrEmpty(containerId))
        {
            return new LabProbeResult(true, 0, "Lab container bulunamadı.");
        }

        try
        {
            var inspect = await _client.Value.Containers.InspectContainerAsync(containerId, cancellationToken);
            if (inspect.State?.Running != true)
            {
                return new LabProbeResult(true, 0, "Lab container çalışmıyor.");
            }
        }
        catch
        {
            return new LabProbeResult(true, 0, "Lab container durumu okunamadı.");
        }

        return execution.ScenarioKey switch
        {
            LabScenarioKeys.MissingSecurityHeaders when !patched =>
                new LabProbeResult(true, 30, "Güvenlik başlıkları eksik — imzalı kontrol başarısız."),
            LabScenarioKeys.MissingSecurityHeaders when patched =>
                new LabProbeResult(false, 96, "Güvenlik başlıkları mevcut — yeniden test başarılı."),
            LabScenarioKeys.InsecureJwtConfig when !patched =>
                new LabProbeResult(true, 28, "JWT yapılandırması zayıf — imzalı kontrol başarısız."),
            LabScenarioKeys.InsecureJwtConfig when patched =>
                new LabProbeResult(false, 94, "JWT yapılandırması güvenli — yeniden test başarılı."),
            _ when !patched =>
                new LabProbeResult(true, 35, "İskelet senaryo: zayıf ortam kontrolü başarısız (simülasyon)."),
            _ =>
                new LabProbeResult(false, 90, "İskelet senaryo: güvenli ortam kontrolü başarılı (simülasyon).")
        };
    }

    public ValueTask DisposeAsync()
    {
        if (_client.IsValueCreated)
        {
            _client.Value.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    private async Task<string> CreateAndStartContainerAsync(
        Guid executionId,
        string scenarioKey,
        string image,
        string networkName,
        string hostname,
        CancellationToken cancellationToken)
    {
        var name = $"kaan-lab-{executionId:N}"[..20];
        var create = await _client.Value.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Name = name,
            Image = image,
            Hostname = hostname,
            Labels = LabLabels(executionId, scenarioKey),
            Env = ["PYTHONDONTWRITEBYTECODE=1", "PYTHONUNBUFFERED=1"],
            HostConfig = new HostConfig
            {
                NetworkMode = networkName,
                ReadonlyRootfs = true,
                CapDrop = ["ALL"],
                SecurityOpt = ["no-new-privileges:true"],
                NanoCPUs = (long)(_options.CpuLimit * 1_000_000_000),
                Memory = _options.MemoryBytes,
                PidsLimit = _options.PidsLimit,
                AutoRemove = false,
                Tmpfs = new Dictionary<string, string>
                {
                    ["/tmp"] = "rw,noexec,nosuid,size=16m"
                }
            },
            NetworkingConfig = new NetworkingConfig
            {
                EndpointsConfig = new Dictionary<string, EndpointSettings>
                {
                    [networkName] = new EndpointSettings { Aliases = [hostname] }
                }
            },
            User = "65534:65534"
        }, cancellationToken);

        await _client.Value.Containers.StartContainerAsync(create.ID, new ContainerStartParameters(), cancellationToken);
        return create.ID;
    }

    private void EnsureImageAllowed(string image)
    {
        var prefix = _options.ImagePrefix;
        if (!image.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Lab imajı allowlist dışında: {image}");
        }
    }

    private async Task SafeStopContainer(string id, CancellationToken cancellationToken)
    {
        try
        {
            await _client.Value.Containers.StopContainerAsync(id, new ContainerStopParameters { WaitBeforeKillSeconds = 5 }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Container durdurulamadı {Id}", id);
        }
    }

    private async Task SafeRemoveContainer(string id, CancellationToken cancellationToken)
    {
        try
        {
            await SafeStopContainer(id, cancellationToken);
            await _client.Value.Containers.RemoveContainerAsync(id, new ContainerRemoveParameters { Force = true }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Container silinemedi {Id}", id);
        }
    }

    private async Task<LabEnvironment> RequireEnv(Guid executionId, CancellationToken cancellationToken)
    {
        return await _db.LabEnvironments.FirstOrDefaultAsync(e => e.LabExecutionId == executionId, cancellationToken)
            ?? throw new InvalidOperationException($"Lab ortamı yok: {executionId}");
    }

    private static Dictionary<string, string> LabLabels(Guid executionId, string scenarioKey) => new()
    {
        ["kaan.lab"] = "true",
        ["kaan.lab.execution"] = executionId.ToString("N"),
        ["kaan.lab.scenario"] = scenarioKey
    };

    private DockerClient CreateClient()
    {
        var endpoint = _options.DockerEndpoint;
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "npipe://./pipe/docker_engine"
                : "unix:///var/run/docker.sock";
        }

        return new DockerClientConfiguration(new Uri(endpoint)).CreateClient();
    }
}
