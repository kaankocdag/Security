using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Projects;

public class AuthorizationRecord : BaseEntity, IAuditableEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid SecurityProjectId { get; set; }
    public Guid DomainAssetId { get; set; }
    public string AuthorizedByName { get; set; } = string.Empty;
    public string AuthorizedByEmail { get; set; } = string.Empty;
    public string? AuthorizedByTitle { get; set; }
    public string AuthorizationScope { get; set; } = string.Empty;
    public string AllowedTestTypes { get; set; } = string.Empty;
    public string? ForbiddenActions { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int MaximumRequestsPerSecond { get; set; } = 1;
    public string? ApprovalEvidencePath { get; set; }
    public AuthorizationStatus Status { get; set; } = AuthorizationStatus.Draft;
    public string? RevocationReason { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public SecurityProject? SecurityProject { get; set; }
    public DomainAsset? DomainAsset { get; set; }
}
