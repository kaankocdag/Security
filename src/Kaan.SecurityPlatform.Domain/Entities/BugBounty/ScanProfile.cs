using Kaan.SecurityPlatform.Domain.Common;

namespace Kaan.SecurityPlatform.Domain.Entities.BugBounty;

/// <summary>
/// AmazonVRP vb. tarama profili. UA / rate-limit değerleri config anahtarlarından okunur; hard-code yok.
/// </summary>
public class ScanProfile : BaseEntity
{
    public string ProfileKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string UserAgentConfigKey { get; set; } = string.Empty;
    public string RateLimitPerMinuteConfigKey { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsEnabled { get; set; } = true;
}
