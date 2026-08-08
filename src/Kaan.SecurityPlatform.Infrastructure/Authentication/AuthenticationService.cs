using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Auth;
using Kaan.SecurityPlatform.Application.Features.Auth.Dtos;
using Kaan.SecurityPlatform.Domain.Entities.Companies;
using Kaan.SecurityPlatform.Domain.Entities.Notifications;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Identity;
using Kaan.SecurityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Authentication;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly SecurityPlatformDbContext _dbContext;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTimeProvider _clock;
    private readonly IActivityEventPublisher _activity;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        SecurityPlatformDbContext dbContext,
        IJwtTokenService jwt,
        IDateTimeProvider clock,
        IActivityEventPublisher activity,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _jwt = jwt;
        _clock = clock;
        _activity = activity;
        _logger = logger;
    }

    public async Task<Result<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return Result<RegisterResponse>.Failure("email_in_use", "Bu e-posta adresi zaten kayıtlı.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = false,
            FirstName = request.FirstName,
            LastName = request.LastName,
            MembershipStatus = MembershipStatus.Pending,
            JobTitle = request.JobTitle,
            CreatedAt = _clock.UtcNow,
            IsActive = true
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var errors = createResult.Errors
                .GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
            return Result<RegisterResponse>.ValidationFailure(errors);
        }

        await EnsureRoleAsync(Roles.CompanyAdmin, cancellationToken);
        await _userManager.AddToRoleAsync(user, Roles.CompanyAdmin);

        var company = new Company
        {
            Name = request.CompanyName,
            ContactName = $"{request.FirstName} {request.LastName}".Trim(),
            ContactEmail = request.Email,
            ContactPhone = request.CompanyContactPhone,
            WebsiteUrl = request.CompanyWebsiteUrl,
            Industry = request.CompanyIndustry,
            Status = CompanyStatus.PendingApproval,
            CreatedAt = _clock.UtcNow,
            CreatedByUserId = user.Id
        };

        _dbContext.Companies.Add(company);
        await _dbContext.SaveChangesAsync(cancellationToken);

        user.PrimaryCompanyId = company.Id;
        await _userManager.UpdateAsync(user);

        _dbContext.CompanyUsers.Add(new CompanyUser
        {
            CompanyId = company.Id,
            UserId = user.Id,
            CompanyRole = CompanyRole.CompanyAdmin,
            IsPrimaryContact = true,
            IsActive = true,
            CreatedAt = _clock.UtcNow
        });

        _dbContext.Notifications.Add(new Notification
        {
            UserId = user.Id,
            CompanyId = company.Id,
            Title = "Kaan Security'ye hoşgeldiniz",
            Message = "Kayıt işleminiz alındı. Hesabınız sistem yöneticisi tarafından incelenip onaylandıktan sonra tüm özellikleri kullanabileceksiniz.",
            Type = NotificationType.Info,
            CreatedAt = _clock.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Yeni üyelik başvurusu: {Email} / firma {Company}", user.Email, company.Name);

        await _activity.PublishToSystemAdminsAsync("membership.requested", new
        {
            userId = user.Id,
            email = user.Email,
            companyId = company.Id,
            companyName = company.Name,
            createdAt = user.CreatedAt
        }, cancellationToken);

        return Result<RegisterResponse>.Success(new RegisterResponse(
            user.Id,
            company.Id,
            user.MembershipStatus,
            "Kayıt başarılı. Sistem yöneticisi hesabınızı onayladıktan sonra giriş yapabilirsiniz."));
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            return Result<AuthResponse>.Failure("invalid_credentials", "E-posta veya parola hatalı.");
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return Result<AuthResponse>.Failure("account_locked", "Hesap geçici olarak kilitlendi. Daha sonra tekrar deneyin.");
        }

        var check = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!check)
        {
            await _userManager.AccessFailedAsync(user);
            return Result<AuthResponse>.Failure("invalid_credentials", "E-posta veya parola hatalı.");
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        user.LastLoginAt = _clock.UtcNow;
        await _userManager.UpdateAsync(user);

        var tokens = await IssueTokensAsync(user, cancellationToken);
        var currentUser = await BuildCurrentUserAsync(user, cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(
            tokens.AccessToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshToken,
            tokens.RefreshTokenExpiresAt,
            tokens.TokenType,
            currentUser));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(
        RefreshRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var outcome = await _jwt.RefreshAsync(request.RefreshToken, ipAddress, cancellationToken);
        if (outcome is null)
        {
            return Result<AuthResponse>.Failure("invalid_refresh_token", "Refresh token geçersiz veya süresi dolmuş.");
        }

        var user = await _userManager.FindByIdAsync(outcome.UserId.ToString());
        if (user is null)
        {
            return Result<AuthResponse>.Failure("invalid_refresh_token", "Refresh token için kullanıcı bulunamadı.");
        }

        var currentUser = await BuildCurrentUserAsync(user, cancellationToken);
        return Result<AuthResponse>.Success(new AuthResponse(
            outcome.Tokens.AccessToken,
            outcome.Tokens.AccessTokenExpiresAt,
            outcome.Tokens.RefreshToken,
            outcome.Tokens.RefreshTokenExpiresAt,
            outcome.Tokens.TokenType,
            currentUser));
    }

    public async Task<Result> RevokeAsync(RevokeRequest request, CancellationToken cancellationToken = default)
    {
        await _jwt.RevokeAsync(request.RefreshToken, cancellationToken);
        return Result.Success();
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }
        return await BuildCurrentUserAsync(user, cancellationToken);
    }

    private async Task<CurrentUserDto> BuildCurrentUserAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        string? companyName = null;
        if (user.PrimaryCompanyId is Guid companyId)
        {
            companyName = await _dbContext.Companies
                .IgnoreQueryFilters()
                .Where(c => c.Id == companyId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new CurrentUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.PrimaryCompanyId,
            companyName,
            user.MembershipStatus,
            roles.ToArray(),
            user.AvatarPath);
    }

    private async Task<AuthenticationTokens> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var extra = new Dictionary<string, string>
        {
            [ClaimTypesExtended.MembershipStatus] = ((int)user.MembershipStatus).ToString(),
            [ClaimTypesExtended.FullName] = user.FullName
        };
        if (user.PrimaryCompanyId is Guid companyId)
        {
            extra[ClaimTypesExtended.CompanyId] = companyId.ToString();
        }

        return await _jwt.IssueAsync(user.Id, user.Email ?? string.Empty, roles, extra, cancellationToken);
    }

    private async Task EnsureRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new ApplicationRole(roleName)
            {
                IsSystemRole = true
            });
        }
    }
}
