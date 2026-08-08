using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Lab;

public class LabExecutionStep : BaseEntity
{
    public Guid LabExecutionId { get; set; }
    public LabExecution? LabExecution { get; set; }

    public LabStepKind StepKind { get; set; }
    public int StepOrder { get; set; }
    public string TitleTr { get; set; } = string.Empty;
    public LabStepStatus Status { get; set; } = LabStepStatus.Pending;
    public string? SummaryTr { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
