using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Features.Admin.Dtos;

public sealed record PendingUserListItem(
    Guid UserId,
    string Email,
    string FullName,
    string? JobTitle,
    Guid? CompanyId,
    string? CompanyName,
    MembershipStatus Status,
    DateTime CreatedAt);

public sealed record ApproveUserRequest(Guid UserId, string? Note);
public sealed record RejectUserRequest(Guid UserId, string Reason);
public sealed record SuspendUserRequest(Guid UserId, string Reason);

public sealed record ApproveCompanyRequest(Guid CompanyId);
public sealed record SuspendCompanyRequest(Guid CompanyId, string Reason);

public sealed record PendingCompanyListItem(
    Guid CompanyId,
    string Name,
    string ContactName,
    string ContactEmail,
    string? Industry,
    CompanyStatus Status,
    DateTime CreatedAt);
