using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Companies;

public class CompanyUser : BaseEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public CompanyRole CompanyRole { get; set; } = CompanyRole.Viewer;
    public bool IsPrimaryContact { get; set; }
    public DateTime? LastAccessAt { get; set; }
    public string? InvitationEmail { get; set; }
    public Guid? InvitedByUserId { get; set; }
    public bool IsActive { get; set; } = true;

    public Company? Company { get; set; }
}
