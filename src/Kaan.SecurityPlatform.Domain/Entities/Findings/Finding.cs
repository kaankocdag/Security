using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.BugBounty;
using Kaan.SecurityPlatform.Domain.Entities.Knowledge;
using Kaan.SecurityPlatform.Domain.Entities.Scans;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Findings;

public class Finding : BaseEntity, IAuditableEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid ScanResultId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? TechnicalDescription { get; set; }
    public string? BusinessImpact { get; set; }
    public Severity Severity { get; set; } = Severity.Informational;
    public ConfidenceLevel ConfidenceLevel { get; set; } = ConfidenceLevel.Recommendation;
    public string Category { get; set; } = string.Empty;
    public string? CweCode { get; set; }
    public string? OwaspCategory { get; set; }
    public string? AffectedUrl { get; set; }
    public string? AffectedParameter { get; set; }
    public string? Evidence { get; set; }
    public string? Remediation { get; set; }
    public string? RemediationExampleConfig { get; set; }
    public string? TurkishExecutiveSummary { get; set; }
    public DateTime FirstSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public FindingStatus Status { get; set; } = FindingStatus.Open;
    public bool IsFalsePositive { get; set; }
    public Guid? VerifiedByAnalystUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string CheckCode { get; set; } = string.Empty;
    public string? Fingerprint { get; set; }
    public int ScoreImpact { get; set; }

    /// <summary>Scanner Severity'den bağımsız teknik şiddet (doğrulama katmanı).</summary>
    public Severity TechnicalSeverity { get; set; } = Severity.Informational;

    /// <summary>Doğrulanmamış XSS vb. için potansiyel teknik şiddet (BB severity değil).</summary>
    public Severity? TechnicalPotentialSeverity { get; set; }

    /// <summary>HackerOne severity — doğrulanmamışsa Unassigned.</summary>
    public BugBountySeverity BugBountySeverity { get; set; } = BugBountySeverity.Unassigned;

    public Exploitability Exploitability { get; set; } = Exploitability.None;
    public bool DemonstratedImpact { get; set; }
    public bool RequiresManualValidation { get; set; } = true;
    public FindingClass FindingClass { get; set; } = FindingClass.Informational;
    public bool BugBountyEligible { get; set; }
    public string? EligibilityReason { get; set; }
    public string? ProgramPolicyMatch { get; set; }
    public SubmissionRecommendation SubmissionRecommendation { get; set; } = SubmissionRecommendation.DoNotSubmit;
    public BugBountyPolicyCategory PolicyCategory { get; set; } = BugBountyPolicyCategory.ScannerOutputOnly;
    public Guid? RootCauseGroupId { get; set; }

    /// <summary>True only after Finding Validation proves unauthorized privileged access.</summary>
    public bool ConfirmedVulnerability { get; set; }

    /// <summary>Cached from latest FindingValidationRun.</summary>
    public ValidationStatus? LatestValidationStatus { get; set; }

    /// <summary>SubmissionEligible from latest validation — never auto-submit to HackerOne.</summary>
    public bool SubmissionEligible { get; set; }

    /// <summary>Potential reward signal only when SubmissionEligible; reward never guaranteed.</summary>
    public bool PotentialRewardEligible { get; set; }

    public Guid? LatestValidationRunId { get; set; }

    // Reflection analyzer metadata (XSS candidate)
    public ReflectionContext? ReflectionContext { get; set; }
    public int? ReflectionCount { get; set; }
    public bool? HtmlEncoded { get; set; }
    public bool? AttributeEncoded { get; set; }
    public string? ReflectionContentType { get; set; }
    public int? ReflectionHttpStatus { get; set; }
    public string? ReflectionLocation { get; set; }
    public string? InputSource { get; set; }
    public string? ReflectionMarker { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public ScanResult? ScanResult { get; set; }
    public RootCauseGroup? RootCauseGroup { get; set; }
    public ICollection<FindingStatusHistory> StatusHistory { get; set; } = new List<FindingStatusHistory>();
    public ICollection<FindingKnowledgeLink> KnowledgeLinks { get; set; } = new List<FindingKnowledgeLink>();
    public ICollection<RemediationRequest> RemediationRequests { get; set; } = new List<RemediationRequest>();
    public ICollection<RetestComparison> RetestComparisons { get; set; } = new List<RetestComparison>();
}
