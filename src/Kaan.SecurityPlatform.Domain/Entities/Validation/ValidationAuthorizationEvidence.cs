using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Projects;

namespace Kaan.SecurityPlatform.Domain.Entities.Validation;

/// <summary>
/// Explicit evidence that the platform user authorized validation against a target.
/// Distinct from TargetInBountyScope — bounty scope alone never authorizes active validation.
/// </summary>
public class ValidationAuthorizationEvidence : BaseEntity, IAuditableEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid TargetId { get; set; }
    public Guid? AuthorizationRecordId { get; set; }
    public string AuthorizedByName { get; set; } = string.Empty;
    public string AuthorizedByEmail { get; set; } = string.Empty;
    public string ScopeSummary { get; set; } = string.Empty;
    public string AllowedTestTypes { get; set; } = "passive,safe-differential";
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public string? EvidenceNotes { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public DomainAsset? Target { get; set; }
    public AuthorizationRecord? AuthorizationRecord { get; set; }
}
