using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.AuthenticatedScanning;

public class ScanModeObservation : BaseEntity
{
    public Guid AuthenticatedScanRunId { get; set; }
    public bool IsAuthenticatedMode { get; set; }
    public Guid? TestAccountId { get; set; }
    public string? MaskedAccountLabel { get; set; }
    public string Url { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? FinalUrl { get; set; }
    public string? RedirectChain { get; set; }
    public string? ContentType { get; set; }
    public string? ResponseHash { get; set; }
    public bool LoginDetected { get; set; }
    public bool AccessDeniedDetected { get; set; }
    public bool AuthenticationConfirmed { get; set; }
    public string? RedactedEvidence { get; set; }
    public AuthScanComparisonResult? ComparisonResult { get; set; }

    public AuthenticatedScanRun? AuthenticatedScanRun { get; set; }
}
