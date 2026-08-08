using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Projects;

namespace Kaan.SecurityPlatform.Domain.Entities.AuthenticatedScanning;

public class TestIdentityProfile : BaseEntity, IAuditableEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid TargetId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public string TargetDomain { get; set; } = string.Empty;
    public string? ProgramName { get; set; }
    public string? ProgramUrl { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Country { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string AccountPurpose { get; set; } = "Security testing only";
    public bool OwnershipConfirmed { get; set; }
    public bool TestingPermissionConfirmed { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public DomainAsset? Target { get; set; }
}
