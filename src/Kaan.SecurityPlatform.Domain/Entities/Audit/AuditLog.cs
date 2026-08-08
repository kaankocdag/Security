using Kaan.SecurityPlatform.Domain.Common;

namespace Kaan.SecurityPlatform.Domain.Entities.Audit;

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public Guid? CompanyId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Details { get; set; }
    public string? Category { get; set; }
    public bool IsSensitive { get; set; }
}
