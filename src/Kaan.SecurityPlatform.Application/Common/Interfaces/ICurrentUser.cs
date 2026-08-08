using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

/// <summary>
/// Oturumdaki kullanıcının bilgilerini Application katmanına sağlayan servis.
/// Api katmanında HttpContext üzerinden, Worker katmanında ise varsayılan sistem
/// hesabı üzerinden implemente edilir.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    Guid? CompanyId { get; }
    bool IsAuthenticated { get; }
    bool IsSystemAdmin { get; }
    MembershipStatus MembershipStatus { get; }
    IReadOnlyCollection<string> Roles { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }

    bool IsInRole(string role);
    bool BelongsToCompany(Guid companyId);
}
