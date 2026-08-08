using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Lab;

public class LabEnvironment : BaseEntity
{
    public Guid LabExecutionId { get; set; }
    public LabExecution? LabExecution { get; set; }

    public LabRuntimeMode RuntimeMode { get; set; }
    public LabEnvironmentStatus Status { get; set; } = LabEnvironmentStatus.Creating;

    public string? NetworkId { get; set; }
    public string? NetworkName { get; set; }
    public string? VulnerableContainerId { get; set; }
    public string? PatchedContainerId { get; set; }
    public string? InternalEndpoint { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? DestroyedAt { get; set; }
}
