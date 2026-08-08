using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Projects;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Validation;

public class TestAccountSession : BaseEntity, IAuditableEntity, ITenantOwnedEntity
{
    public Guid CompanyId { get; set; }
    public Guid TargetId { get; set; }
    public string Label { get; set; } = string.Empty;
    public ValidationSessionRole Role { get; set; } = ValidationSessionRole.TestAccountA;
    public bool OwnershipConfirmed { get; set; }
    public bool TestingPermissionConfirmed { get; set; }
    /// <summary>DataProtection reference — never store plaintext secrets.</summary>
    public string EncryptedSecretReference { get; set; } = string.Empty;
    /// <summary>Optional ownership hint for differential tests (user-created test resource URL/id).</summary>
    public string? OwnedTestResourceHint { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public DomainAsset? Target { get; set; }
}
