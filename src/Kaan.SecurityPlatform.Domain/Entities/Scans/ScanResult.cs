using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Findings;

namespace Kaan.SecurityPlatform.Domain.Entities.Scans;

public class ScanResult : BaseEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid ScanJobId { get; set; }
    public int SecurityScore { get; set; }
    public int PreviousSecurityScore { get; set; }
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public int InfoCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int StrongIndicationCount { get; set; }
    public int RecommendationCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public string? Summary { get; set; }
    public string? ExecutiveSummary { get; set; }
    public string? TechnicalSummary { get; set; }
    public int ChecksTotal { get; set; }
    public int ChecksPassed { get; set; }
    public int ChecksFailed { get; set; }
    public int ChecksSkipped { get; set; }

    public ScanJob? ScanJob { get; set; }
    public ICollection<Finding> Findings { get; set; } = new List<Finding>();
}
