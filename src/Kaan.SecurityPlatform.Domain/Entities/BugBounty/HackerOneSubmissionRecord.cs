using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.BugBounty;

public class HackerOneSubmissionRecord : BaseEntity, IAuditableEntity
{
    public Guid HackerOneReportDraftId { get; set; }
    public string? ExternalReportId { get; set; }
    public string? ExternalReportUrl { get; set; }
    public HackerOneSubmissionStatus Status { get; set; } = HackerOneSubmissionStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public HackerOneReportDraft? Draft { get; set; }
}
