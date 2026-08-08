using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.BugBounty;

public class BugBountyProgram : BaseEntity
{
    public string PolicyKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Handle { get; set; } = string.Empty;
    public BugBountyPlatform Platform { get; set; } = BugBountyPlatform.HackerOne;
    public string? OpenReportUrl { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? ExternalProgramId { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public bool OffersBounties { get; set; }
    public string? Currency { get; set; }
    public string? SubmissionState { get; set; }
    public bool OpenScope { get; set; }
    public string? State { get; set; }

    public ICollection<BugBountyPolicyRule> PolicyRules { get; set; } = new List<BugBountyPolicyRule>();
    public ICollection<HackerOneReportDraft> ReportDrafts { get; set; } = new List<HackerOneReportDraft>();
}
