using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Kaan.SecurityPlatform.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
        ? Email ?? UserName ?? "-"
        : $"{FirstName} {LastName}".Trim();

    public MembershipStatus MembershipStatus { get; set; } = MembershipStatus.Pending;
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? SuspensionReason { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? PreferredLanguage { get; set; } = "tr-TR";
    public string? AvatarPath { get; set; }
    public string? JobTitle { get; set; }
    public string? PhoneCountryCode { get; set; }

    public Guid? PrimaryCompanyId { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
