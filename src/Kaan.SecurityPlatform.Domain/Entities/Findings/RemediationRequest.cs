using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Companies;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Findings;

public class RemediationRequest : BaseEntity, IAuditableEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid FindingId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public RemediationStatus Status { get; set; } = RemediationStatus.New;
    public Guid? AssignedToUserId { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public string? Currency { get; set; } = "TRY";
    public string? Description { get; set; }
    public string? InternalNotes { get; set; }
    public string ContactPreference { get; set; } = "email";
    public DateTime? CompletedAt { get; set; }
    public string? CompletionNote { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public Finding? Finding { get; set; }
    public Company? Company { get; set; }
}
