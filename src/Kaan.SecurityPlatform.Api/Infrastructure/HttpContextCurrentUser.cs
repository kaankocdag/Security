using System.Security.Claims;
using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Api.Infrastructure;

public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email) ?? Principal?.FindFirstValue("email");
    public string? FullName => Principal?.FindFirstValue(ClaimTypesExtended.FullName);

    public Guid? CompanyId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypesExtended.CompanyId);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
    public bool IsSystemAdmin => Principal?.IsInRole(Application.Authorization.Roles.SystemAdmin) ?? false;

    public MembershipStatus MembershipStatus
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypesExtended.MembershipStatus);
            return int.TryParse(raw, out var value)
                ? (MembershipStatus)value
                : MembershipStatus.Pending;
        }
    }

    public IReadOnlyCollection<string> Roles => Principal?.Claims
        .Where(c => c.Type == ClaimTypes.Role)
        .Select(c => c.Value)
        .ToArray() ?? Array.Empty<string>();

    public string? IpAddress => _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => _accessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public bool IsInRole(string role) => Principal?.IsInRole(role) ?? false;
    public bool BelongsToCompany(Guid companyId) => CompanyId == companyId || IsSystemAdmin;
}
