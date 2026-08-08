using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Lab;

/// <summary>
/// Platform-genel lab oturumu. ITenantOwnedEntity değildir — yalnızca SystemAdmin.
/// </summary>
public class LabExecution : BaseEntity
{
    public string ScenarioKey { get; set; } = string.Empty;
    public AssessmentMode AssessmentMode { get; set; } = AssessmentMode.IsolatedSecurityLab;
    public LabExecutionStatus Status { get; set; } = LabExecutionStatus.Queued;
    public LabRuntimeMode RuntimeMode { get; set; }

    /// <summary>Allowlist hedef site (serbest URL değil).</summary>
    public Guid LabTargetSiteId { get; set; }
    public string TargetHostName { get; set; } = string.Empty;

    public Guid ElevatedByUserId { get; set; }
    public string ElevatedByEmail { get; set; } = string.Empty;
    public Guid AuditCorrelationId { get; set; }
    public Guid? ElevationTicketId { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? FailureReasonTr { get; set; }
    public string? CancelReasonTr { get; set; }

    public LabEnvironment? Environment { get; set; }
    public LabAuthorizationApproval? Approval { get; set; }
    public LabComparisonResult? Comparison { get; set; }
    public ICollection<LabExecutionStep> Steps { get; set; } = new List<LabExecutionStep>();
    public ICollection<LabExecutionLog> Logs { get; set; } = new List<LabExecutionLog>();
}
