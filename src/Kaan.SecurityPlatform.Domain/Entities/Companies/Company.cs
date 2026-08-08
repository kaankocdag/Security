using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Audit;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Entities.Notifications;
using Kaan.SecurityPlatform.Domain.Entities.Projects;
using Kaan.SecurityPlatform.Domain.Entities.Subscriptions;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Companies;

public class Company : BaseEntity, IAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? TaxNumber { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Industry { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public CompanyStatus Status { get; set; } = CompanyStatus.PendingApproval;
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? SuspensionReason { get; set; }
    public string? Notes { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public ICollection<CompanyUser> Members { get; set; } = new List<CompanyUser>();
    public ICollection<SecurityProject> Projects { get; set; } = new List<SecurityProject>();
    public ICollection<CompanySubscription> Subscriptions { get; set; } = new List<CompanySubscription>();
    public ICollection<RemediationRequest> RemediationRequests { get; set; } = new List<RemediationRequest>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
