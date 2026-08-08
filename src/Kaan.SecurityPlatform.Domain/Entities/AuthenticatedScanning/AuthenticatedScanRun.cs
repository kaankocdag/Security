using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Projects;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.AuthenticatedScanning;

public class AuthenticatedScanRun : BaseEntity, IAuditableEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid TargetId { get; set; }
    public Guid? TestAccountId { get; set; }
    public AuthenticatedScanRunStatus Status { get; set; } = AuthenticatedScanRunStatus.NotStarted;
    public ManualTakeoverReason TakeoverReason { get; set; } = ManualTakeoverReason.None;
    public string? TakeoverMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? RequestedBy { get; set; }
    public DateTime? UserApprovedAt { get; set; }
    public int MaxRequestCount { get; set; } = 25;
    public int ActualRequestCount { get; set; }
    public string? StopReason { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool HeadedBrowser { get; set; } = true;
    public string? LoginUrlUsed { get; set; }
    public bool AuthenticationConfirmed { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public DomainAsset? Target { get; set; }
    public TargetTestAccount? TestAccount { get; set; }
    public ICollection<ScanModeObservation> Observations { get; set; } = new List<ScanModeObservation>();
}
