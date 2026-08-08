using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Infrastructure.Services;

/// <summary>
/// Worker ve arka plan işlemleri için sistem kullanıcısı temsili.
/// Global query filter'ları bypass etmesi için IsSystemAdmin = true olarak
/// işaretlenir. Böylece worker firmalar arası veriye erişebilir.
/// </summary>
public sealed class WorkerCurrentUser : ICurrentUser
{
    public Guid? UserId => null;
    public string? Email => "worker@kaansecurity.local";
    public string? FullName => "Kaan Security Worker";
    public Guid? CompanyId => null;
    public bool IsAuthenticated => true;
    public bool IsSystemAdmin => true;
    public MembershipStatus MembershipStatus => MembershipStatus.Approved;
    public IReadOnlyCollection<string> Roles { get; } = new[] { "SystemAdmin" };
    public string? IpAddress => null;
    public string? UserAgent => "KaanSecurityWorker";

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    public bool BelongsToCompany(Guid companyId) => true;
}
