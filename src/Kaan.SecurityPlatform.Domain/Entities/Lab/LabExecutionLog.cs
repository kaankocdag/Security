using Kaan.SecurityPlatform.Domain.Common;

namespace Kaan.SecurityPlatform.Domain.Entities.Lab;

/// <summary>
/// Sanitize edilmiş log satırı — payload / secret içermez.
/// </summary>
public class LabExecutionLog : BaseEntity
{
    public Guid LabExecutionId { get; set; }
    public LabExecution? LabExecution { get; set; }

    public Guid? LabExecutionStepId { get; set; }
    public string Level { get; set; } = "Info";
    public string MessageTr { get; set; } = string.Empty;
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}
