using Kaan.SecurityPlatform.Domain.Common;

namespace Kaan.SecurityPlatform.Domain.Entities.Lab;

public class LabComparisonResult : BaseEntity
{
    public Guid LabExecutionId { get; set; }
    public LabExecution? LabExecution { get; set; }

    public bool InitialTestFailed { get; set; }
    public bool RetestSucceeded { get; set; }
    public int VulnerableScore { get; set; }
    public int PatchedScore { get; set; }
    public string RiskTr { get; set; } = string.Empty;
    public string WhyTr { get; set; } = string.Empty;
    public string FixTr { get; set; } = string.Empty;
    public string SummaryTr { get; set; } = string.Empty;
}
