using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Projects;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.AuthenticatedScanning;

/// <summary>
/// User-owned security test account for a target. Secrets live only as vault references.
/// </summary>
public class TargetTestAccount : BaseEntity, IAuditableEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid TargetId { get; set; }
    public string TargetDomain { get; set; } = string.Empty;
    public string Label { get; set; } = "Security Test Account";
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? DisplayName { get; set; }
    public string EncryptedSecretReference { get; set; } = string.Empty;
    public TestAccountStatus AccountStatus { get; set; } = TestAccountStatus.PendingVerification;
    public TestAccountVerificationStatus VerificationStatus { get; set; } = TestAccountVerificationStatus.NotVerified;
    public string? RegistrationUrl { get; set; }
    public string? LoginUrl { get; set; }
    public DateTime? LastSuccessfulLoginAt { get; set; }
    public DateTime? LastAuthenticatedScanAt { get; set; }
    public bool OwnershipConfirmed { get; set; }
    public bool TestingPermissionConfirmed { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public ValidationSessionRole Role { get; set; } = ValidationSessionRole.TestAccountA;
    public Guid? IdentityProfileId { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public DomainAsset? Target { get; set; }
    public TestIdentityProfile? IdentityProfile { get; set; }
}
