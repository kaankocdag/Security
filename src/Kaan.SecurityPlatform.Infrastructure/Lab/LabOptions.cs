namespace Kaan.SecurityPlatform.Infrastructure.Lab;

public sealed class LabOptions
{
    public const string SectionName = "Lab";

    /// <summary>Auto | Docker | Mock</summary>
    public string RuntimeMode { get; set; } = "Auto";
    public int ElevationMinutes { get; set; } = 10;
    public int ExecutionTimeoutMinutes { get; set; } = 15;
    public string DockerEndpoint { get; set; } = "npipe://./pipe/docker_engine";
    public string ImagePrefix { get; set; } = "kaan-lab/";
    public string InternalNetworkName { get; set; } = "kaan-lab-net";
    /// <summary>IsolatedSecurityLab denemeleri için lab ağında internet çıkışı açık.</summary>
    public bool AllowEgress { get; set; } = true;
    public int MaxHttpRequestsPerExecution { get; set; } = 40;
    public double CpuLimit { get; set; } = 0.5;
    public long MemoryBytes { get; set; } = 256 * 1024 * 1024;
    public long PidsLimit { get; set; } = 64;
}
