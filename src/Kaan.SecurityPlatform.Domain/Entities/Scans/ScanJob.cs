using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Projects;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Scans;

public class ScanJob : BaseEntity, IAuditableEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid SecurityProjectId { get; set; }
    public Guid DomainAssetId { get; set; }
    public ScanType ScanType { get; set; } = ScanType.FullPassive;
    /// <summary>PublicPassiveAssessment veya AuthorizedExternalAssessment (lab ayrı pipeline).</summary>
    public AssessmentMode AssessmentMode { get; set; } = AssessmentMode.PublicPassiveAssessment;
    public ScanStatus Status { get; set; } = ScanStatus.Queued;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int ProgressPercentage { get; set; }
    public string? CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public int CompletedSteps { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string? ErrorMessage { get; set; }
    public string ScannerVersion { get; set; } = "1.0.0";
    public string? HangfireJobId { get; set; }
    public Guid? PreviousScanJobId { get; set; }
    public bool IsRetest { get; set; }
    public Guid? RetestForFindingId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public SecurityProject? SecurityProject { get; set; }
    public DomainAsset? DomainAsset { get; set; }
    public ScanResult? Result { get; set; }
    public ScanJob? PreviousScanJob { get; set; }
}
