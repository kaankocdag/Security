using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Projects;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Validation;

public class ScopePolicy : BaseEntity, IAuditableEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid TargetId { get; set; }
    public string ProgramName { get; set; } = string.Empty;
    public string? ProgramUrl { get; set; }
    public ScopePolicyStatus ScopeStatus { get; set; } = ScopePolicyStatus.Unverified;
    /// <summary>Comma-separated allowed methods, e.g. GET,HEAD,OPTIONS</summary>
    public string AllowedTestMethods { get; set; } = "GET,HEAD,OPTIONS";
    /// <summary>Comma-separated prohibited methods/tests</summary>
    public string ProhibitedTestMethods { get; set; } =
        "POST,PUT,PATCH,DELETE,BRUTE_FORCE,CREDENTIAL_STUFFING,AUTH_BYPASS,SQLI,RCE,SSRF,XXE";
    public int RateLimit { get; set; } = 1;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public string? PolicyEvidence { get; set; }
    public bool TargetInBountyScope { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public DomainAsset? Target { get; set; }
}
