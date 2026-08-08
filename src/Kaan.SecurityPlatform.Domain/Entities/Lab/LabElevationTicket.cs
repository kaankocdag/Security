using Kaan.SecurityPlatform.Domain.Common;

namespace Kaan.SecurityPlatform.Domain.Entities.Lab;

/// <summary>
/// Step-up parola sonrası kısa ömürlü elevation bileti.
/// </summary>
public class LabElevationTicket : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public string ClientIp { get; set; } = string.Empty;
    public bool IsRevoked { get; set; }
}
