using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Companies;
using Kaan.SecurityPlatform.Domain.Entities.Scans;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Projects;

public class SecurityProject : BaseEntity, IAuditableEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public EnvironmentType EnvironmentType { get; set; } = EnvironmentType.Production;
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public string? PrimaryContactEmail { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public Company? Company { get; set; }
    public ICollection<DomainAsset> Domains { get; set; } = new List<DomainAsset>();
    public ICollection<ScanJob> ScanJobs { get; set; } = new List<ScanJob>();
    public ICollection<AuthorizationRecord> Authorizations { get; set; } = new List<AuthorizationRecord>();
}
