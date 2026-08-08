using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Entities.Projects;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Validation;

public class FindingValidationRun : BaseEntity, IAuditableEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid FindingId { get; set; }
    public Guid TargetId { get; set; }
    public string ValidatorType { get; set; } = string.Empty;
    public ValidationMode ValidationMode { get; set; } = ValidationMode.PassiveReadOnly;
    public ValidationStatus Status { get; set; } = ValidationStatus.NotStarted;
    public ValidationRiskLevel RiskLevel { get; set; } = ValidationRiskLevel.Low;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? RequestedBy { get; set; }
    public DateTime? UserApprovedAt { get; set; }
    public Guid? AuthorizationEvidenceId { get; set; }
    public Guid? ScopePolicyId { get; set; }
    public int MaxRequestCount { get; set; } = 10;
    public int ActualRequestCount { get; set; }
    public string? StopReason { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public bool StopRequested { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public Finding? Finding { get; set; }
    public DomainAsset? Target { get; set; }
    public ValidationAuthorizationEvidence? AuthorizationEvidence { get; set; }
    public ScopePolicy? ScopePolicy { get; set; }
    public FindingValidationResult? Result { get; set; }
    public ICollection<ValidationEvidence> EvidenceItems { get; set; } = new List<ValidationEvidence>();
}
