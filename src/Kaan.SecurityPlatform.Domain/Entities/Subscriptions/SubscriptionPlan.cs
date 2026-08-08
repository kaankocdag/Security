using Kaan.SecurityPlatform.Domain.Common;

namespace Kaan.SecurityPlatform.Domain.Entities.Subscriptions;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int MonthlyScanLimit { get; set; }
    public int MaxProjects { get; set; }
    public int MaxDomains { get; set; }
    public bool ScheduledScanningEnabled { get; set; }
    public bool SourceCodeScanningEnabled { get; set; }
    public bool ReportExportEnabled { get; set; }
    public bool PriorityQueueEnabled { get; set; }
    public decimal MonthlyPrice { get; set; }
    public string Currency { get; set; } = "TRY";
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ICollection<CompanySubscription> Subscriptions { get; set; } = new List<CompanySubscription>();
}
