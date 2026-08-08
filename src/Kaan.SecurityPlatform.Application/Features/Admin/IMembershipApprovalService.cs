using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Admin.Dtos;

namespace Kaan.SecurityPlatform.Application.Features.Admin;

public interface IMembershipApprovalService
{
    Task<IReadOnlyList<PendingUserListItem>> ListPendingUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingCompanyListItem>> ListPendingCompaniesAsync(CancellationToken cancellationToken = default);
    Task<Result> ApproveUserAsync(ApproveUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> RejectUserAsync(RejectUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> SuspendUserAsync(SuspendUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> ApproveCompanyAsync(ApproveCompanyRequest request, CancellationToken cancellationToken = default);
    Task<Result> SuspendCompanyAsync(SuspendCompanyRequest request, CancellationToken cancellationToken = default);
}
