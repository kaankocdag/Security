using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Infrastructure.Services;

/// <summary>
/// Kimlik doğrulama yapılmamış istekler ve tasarım zamanı için varsayılan.
/// </summary>
public sealed class AnonymousCurrentUser : ICurrentUser
{
    public Guid? UserId => null;
    public string? Email => null;
    public string? FullName => null;
    public Guid? CompanyId => null;
    public bool IsAuthenticated => false;
    public bool IsSystemAdmin => false;
    public MembershipStatus MembershipStatus => MembershipStatus.Pending;
    public IReadOnlyCollection<string> Roles { get; } = Array.Empty<string>();
    public string? IpAddress => null;
    public string? UserAgent => null;

    public bool IsInRole(string role) => false;
    public bool BelongsToCompany(Guid companyId) => false;
}
