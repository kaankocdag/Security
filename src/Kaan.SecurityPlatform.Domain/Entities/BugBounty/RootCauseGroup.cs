using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Findings;

namespace Kaan.SecurityPlatform.Domain.Entities.BugBounty;

public class RootCauseGroup : BaseEntity
{
    public string FingerprintKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int FindingCount { get; set; }

    public ICollection<Finding> Findings { get; set; } = new List<Finding>();
}
