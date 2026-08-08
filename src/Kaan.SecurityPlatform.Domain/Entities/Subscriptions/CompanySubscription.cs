using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Companies;

namespace Kaan.SecurityPlatform.Domain.Entities.Subscriptions;

public class CompanySubscription : BaseEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid SubscriptionPlanId { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string Status { get; set; } = "Active";
    public string? Notes { get; set; }
    public int UsedScansThisPeriod { get; set; }
    public DateTime CurrentPeriodStartsAt { get; set; }
    public DateTime CurrentPeriodEndsAt { get; set; }

    public Company? Company { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }
}
