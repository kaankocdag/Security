using Kaan.SecurityPlatform.Domain.Common;

namespace Kaan.SecurityPlatform.Domain.Entities.BugBounty;

/// <summary>Data Protection ile şifrelenmiş HackerOne API kimlik bilgisi (plaintext saklanmaz).</summary>
public class HackerOneApiCredential : BaseEntity, IAuditableEntity
{
    public string Identifier { get; set; } = "default";
    public string ProtectedApiToken { get; set; } = string.Empty;
    public string? ApiUsername { get; set; }
    public DateTime? LastValidatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}