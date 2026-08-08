using Kaan.SecurityPlatform.Domain.Common;

namespace Kaan.SecurityPlatform.Domain.Entities.Lab;

public class LabAuthorizationApproval : BaseEntity
{
    public Guid LabExecutionId { get; set; }
    public LabExecution? LabExecution { get; set; }

    public Guid UserId { get; set; }
    public string ConfirmPhrase { get; set; } = string.Empty;
    public string ClientIp { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime ApprovedAt { get; set; } = DateTime.UtcNow;
}
