using Kaan.SecurityPlatform.Domain.Common;

namespace Kaan.SecurityPlatform.Domain.Entities.Lab;

/// <summary>
/// IsolatedSecurityLab için SystemAdmin tarafından eklenen allowlist hedefi.
/// Serbest URL girişi yoktur; yalnızca bu kayıtlardan seçim yapılır.
/// </summary>
public class LabTargetSite : BaseEntity
{
    public string HostName { get; set; } = string.Empty;
    public string NormalizedHostName { get; set; } = string.Empty;
    public string? NotesTr { get; set; }
    public bool IsEnabled { get; set; } = true;
    public Guid CreatedByUserId { get; set; }
    public string CreatedByEmail { get; set; } = string.Empty;
}
