using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Scans;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Projects;

public class DomainAsset : BaseEntity, IAuditableEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid SecurityProjectId { get; set; }
    public string HostName { get; set; } = string.Empty;
    public string NormalizedHostName { get; set; } = string.Empty;
    public string Scheme { get; set; } = "https";
    public int? Port { get; set; }
    public bool IsVerified { get; set; }
    public VerificationMethod? VerificationMethod { get; set; }
    public string? VerificationToken { get; set; }
    public DateTime? VerificationTokenCreatedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? LastVerificationError { get; set; }
    public DomainAssetStatus Status { get; set; } = DomainAssetStatus.PendingVerification;
    public string? Notes { get; set; }

    /// <summary>Manual | HackerOne</summary>
    public string Source { get; set; } = "Manual";
    public string? HackerOneProgramHandle { get; set; }
    public string? HackerOneProgramName { get; set; }
    public string? HackerOneScopeId { get; set; }
    public string? HackerOneAssetType { get; set; }
    public bool? HackerOneEligibleForBounty { get; set; }
    public bool? HackerOneEligibleForSubmission { get; set; }
    public string? HackerOneMaxSeverity { get; set; }
    public bool? HackerOneOffersBounties { get; set; }
    public string? HackerOneCurrency { get; set; }
    public string? HackerOneSubmissionState { get; set; }
    public bool HackerOneIsWildcard { get; set; }
    /// <summary>Human-readable bounty hint (API exact $ amounts are usually not provided).</summary>
    public string? HackerOneBountySummary { get; set; }
    public DateTime? HackerOneLastSyncedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public SecurityProject? SecurityProject { get; set; }
    public ICollection<ScanJob> ScanJobs { get; set; } = new List<ScanJob>();
    public ICollection<AuthorizationRecord> Authorizations { get; set; } = new List<AuthorizationRecord>();
}
