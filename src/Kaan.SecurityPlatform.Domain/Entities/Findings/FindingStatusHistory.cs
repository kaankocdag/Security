using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Findings;

public class FindingStatusHistory : BaseEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid FindingId { get; set; }
    public FindingStatus PreviousStatus { get; set; }
    public FindingStatus NewStatus { get; set; }
    public Guid ChangedByUserId { get; set; }
    public string? Note { get; set; }

    public Finding? Finding { get; set; }
}
