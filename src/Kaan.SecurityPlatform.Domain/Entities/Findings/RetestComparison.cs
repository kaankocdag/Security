using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Scans;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Findings;

public class RetestComparison : BaseEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid OriginalFindingId { get; set; }
    public Guid PreviousScanResultId { get; set; }
    public Guid CurrentScanResultId { get; set; }
    public Severity PreviousSeverity { get; set; }
    public Severity? CurrentSeverity { get; set; }
    public ConfidenceLevel PreviousConfidence { get; set; }
    public ConfidenceLevel? CurrentConfidence { get; set; }
    public RetestResult Result { get; set; } = RetestResult.UnableToVerify;
    public string? ComparisonSummary { get; set; }
    public Guid RequestedByUserId { get; set; }

    public Finding? OriginalFinding { get; set; }
    public ScanResult? PreviousScanResult { get; set; }
    public ScanResult? CurrentScanResult { get; set; }
}
