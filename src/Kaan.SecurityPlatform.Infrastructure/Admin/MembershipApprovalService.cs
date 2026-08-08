using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Admin;
using Kaan.SecurityPlatform.Application.Features.Admin.Dtos;
using Kaan.SecurityPlatform.Domain.Entities.Notifications;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Identity;
using Kaan.SecurityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kaan.SecurityPlatform.Infrastructure.Admin;

public sealed class MembershipApprovalService : IMembershipApprovalService
{
    private readonly SecurityPlatformDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IActivityEventPublisher _activity;
    private readonly IAuditLogWriter _audit;

    public MembershipApprovalService(
        SecurityPlatformDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IActivityEventPublisher activity,
        IAuditLogWriter audit)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _currentUser = currentUser;
        _clock = clock;
        _activity = activity;
        _audit = audit;
    }

    public async Task<IReadOnlyList<PendingUserListItem>> ListPendingUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .Where(u => u.MembershipStatus == MembershipStatus.Pending)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.JobTitle,
                u.PrimaryCompanyId,
                u.MembershipStatus,
                u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var companyIds = users.Select(u => u.PrimaryCompanyId).Where(x => x is not null).Cast<Guid>().ToArray();
        var companyNames = await _dbContext.Companies
            .IgnoreQueryFilters()
            .Where(c => companyIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        return users.Select(u => new PendingUserListItem(
            u.Id,
            u.Email ?? string.Empty,
            $"{u.FirstName} {u.LastName}".Trim(),
            u.JobTitle,
            u.PrimaryCompanyId,
            u.PrimaryCompanyId is Guid cid && companyNames.TryGetValue(cid, out var name) ? name : null,
            u.MembershipStatus,
            u.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<PendingCompanyListItem>> ListPendingCompaniesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Companies
            .IgnoreQueryFilters()
            .Where(c => c.Status == CompanyStatus.PendingApproval)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new PendingCompanyListItem(
                c.Id,
                c.Name,
                c.ContactName,
                c.ContactEmail,
                c.Industry,
                c.Status,
                c.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result> ApproveUserAsync(ApproveUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return Result.Failure("user_not_found", "Kullanıcı bulunamadı.");
        }

        user.MembershipStatus = MembershipStatus.Approved;
        user.ApprovedAt = _clock.UtcNow;
        user.ApprovedByUserId = _currentUser.UserId;
        await _userManager.UpdateAsync(user);

        if (user.PrimaryCompanyId is Guid companyId)
        {
            var company = await _dbContext.Companies
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
            if (company is not null && company.Status == CompanyStatus.PendingApproval)
            {
                company.Status = CompanyStatus.Active;
                company.ApprovedAt = _clock.UtcNow;
                company.ApprovedByUserId = _currentUser.UserId;
            }
        }

        _dbContext.Notifications.Add(new Notification
        {
            UserId = user.Id,
            CompanyId = user.PrimaryCompanyId,
            Title = "Üyeliğiniz onaylandı",
            Message = "Sistem yöneticisi hesabınızı onayladı. Artık projeler oluşturup tarama başlatabilirsiniz.",
            Type = NotificationType.MembershipApproved,
            CreatedAt = _clock.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(
            "membership.approve",
            "ApplicationUser",
            user.Id.ToString(),
            new { note = request.Note },
            category: "membership",
            isSensitive: true,
            cancellationToken: cancellationToken);

        await _activity.PublishToUserAsync(user.Id, "membership.approved", new { userId = user.Id }, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RejectUserAsync(RejectUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return Result.Failure("user_not_found", "Kullanıcı bulunamadı.");
        }

        user.MembershipStatus = MembershipStatus.Rejected;
        user.RejectionReason = request.Reason;
        user.IsActive = false;
        await _userManager.UpdateAsync(user);

        _dbContext.Notifications.Add(new Notification
        {
            UserId = user.Id,
            Title = "Üyelik başvurunuz reddedildi",
            Message = $"Başvurunuz reddedildi. Gerekçe: {request.Reason}",
            Type = NotificationType.MembershipRejected,
            CreatedAt = _clock.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(
            "membership.reject",
            "ApplicationUser",
            user.Id.ToString(),
            new { request.Reason },
            category: "membership",
            isSensitive: true,
            cancellationToken: cancellationToken);
        return Result.Success();
    }

    public async Task<Result> SuspendUserAsync(SuspendUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null)
        {
            return Result.Failure("user_not_found", "Kullanıcı bulunamadı.");
        }

        user.MembershipStatus = MembershipStatus.Suspended;
        user.SuspensionReason = request.Reason;
        user.IsActive = false;
        await _userManager.UpdateAsync(user);
        await _audit.WriteAsync(
            "membership.suspend",
            "ApplicationUser",
            user.Id.ToString(),
            new { request.Reason },
            category: "membership",
            isSensitive: true,
            cancellationToken: cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ApproveCompanyAsync(ApproveCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.Companies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (company is null)
        {
            return Result.Failure("company_not_found", "Firma bulunamadı.");
        }

        company.Status = CompanyStatus.Active;
        company.ApprovedAt = _clock.UtcNow;
        company.ApprovedByUserId = _currentUser.UserId;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(
            "company.approve",
            "Company",
            company.Id.ToString(),
            null,
            category: "membership",
            cancellationToken: cancellationToken);
        return Result.Success();
    }

    public async Task<Result> SuspendCompanyAsync(SuspendCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.Companies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (company is null)
        {
            return Result.Failure("company_not_found", "Firma bulunamadı.");
        }

        company.Status = CompanyStatus.Suspended;
        company.SuspensionReason = request.Reason;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync(
            "company.suspend",
            "Company",
            company.Id.ToString(),
            new { request.Reason },
            category: "membership",
            isSensitive: true,
            cancellationToken: cancellationToken);
        return Result.Success();
    }
}
