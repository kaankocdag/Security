using Kaan.SecurityPlatform.Domain.Common;

namespace Kaan.SecurityPlatform.Domain.Entities.BugBounty;

public class BugBountyAuditLog : BaseEntity
{
    public Guid? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? DetailsJson { get; set; }
}
