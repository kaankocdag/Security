using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning;
using Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning.Dtos;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Kaan.SecurityPlatform.Application.Features.Validation;
using Kaan.SecurityPlatform.Domain.Entities.AuthenticatedScanning;
using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Kaan.SecurityPlatform.Infrastructure.AuthenticatedScanning;

public sealed class TestAccountManagementService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    ITestAccountVault vault,
    IScopePolicyValidator scopeValidator,
    IAuthorizationEvidenceService authEvidence,
    IRegistrationPageDetector registrationDetector,
    IHostEnvironment env,
    IBugBountyAuditWriter audit) : ITestAccountManagementService
{
    private const int MaxAccountsPerTarget = 2;

    public async Task<IReadOnlyList<TargetTestAccountDto>> ListAsync(
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        var list = await db.TargetTestAccounts.AsNoTracking()
            .Where(a => a.TargetId == targetId)
            .OrderBy(a => a.Role)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<Result<TargetTestAccountDto>> RegisterExistingAsync(
        RegisterExistingTestAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.CompanyId is null || currentUser.UserId is null)
        {
            return Result<TargetTestAccountDto>.Failure("no_tenant", "Şirket/kullanıcı bağlamı yok.");
        }

        if (!request.OwnershipConfirmed || !request.TestingPermissionConfirmed)
        {
            return Result<TargetTestAccountDto>.Failure(
                "ownership_required",
                "OwnershipConfirmed ve TestingPermissionConfirmed zorunlu.");
        }

        var gate = await EnsureTargetAuthorizedAsync(request.TargetId, cancellationToken);
        if (!gate.IsSuccess)
        {
            return Result<TargetTestAccountDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var count = await db.TargetTestAccounts.CountAsync(
            a => a.TargetId == request.TargetId && a.IsActive, cancellationToken);
        if (count >= MaxAccountsPerTarget)
        {
            return Result<TargetTestAccountDto>.Failure(
                "max_accounts",
                $"Hedef başına en fazla {MaxAccountsPerTarget} aktif test hesabı.");
        }

        var domain = await db.DomainAssets.AsNoTracking()
            .Where(d => d.Id == request.TargetId)
            .Select(d => d.HostName)
            .FirstAsync(cancellationToken);

        var entity = new TargetTestAccount
        {
            CompanyId = currentUser.CompanyId.Value,
            TargetId = request.TargetId,
            TargetDomain = domain,
            Label = string.IsNullOrWhiteSpace(request.Label) ? "Security Test Account" : request.Label.Trim(),
            Email = request.Email.Trim(),
            Username = request.Username?.Trim(),
            DisplayName = request.DisplayName?.Trim() ?? "Security Test",
            EncryptedSecretReference = vault.ProtectPassword(request.Password),
            AccountStatus = TestAccountStatus.Active,
            VerificationStatus = TestAccountVerificationStatus.Verified,
            LoginUrl = request.LoginUrl,
            OwnershipConfirmed = true,
            TestingPermissionConfirmed = true,
            Role = request.Role,
            CreatedBy = currentUser.UserId.Value,
            IsActive = true
        };
        db.TargetTestAccounts.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("authscan.test_account.registered_existing", "TargetTestAccount", entity.Id.ToString(),
            new { entity.TargetId, entity.Label, entity.Email, entity.Role }, cancellationToken);
        return Result<TargetTestAccountDto>.Success(Map(entity));
    }

    public async Task<Result<Guid>> CreateIdentityProfileAsync(
        UpsertTestIdentityProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.CompanyId is null || currentUser.UserId is null)
        {
            return Result<Guid>.Failure("no_tenant", "Şirket/kullanıcı bağlamı yok.");
        }

        if (!request.OwnershipConfirmed || !request.TestingPermissionConfirmed)
        {
            return Result<Guid>.Failure("ownership_required", "Ownership / testing permission onayı zorunlu.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return Result<Guid>.Failure("email_required", "Kullanıcı kendi kontrolündeki test e-postasını sağlamalıdır.");
        }

        var gate = await EnsureTargetAuthorizedAsync(request.TargetId, cancellationToken);
        if (!gate.IsSuccess)
        {
            return Result<Guid>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var domain = await db.DomainAssets.AsNoTracking()
            .Where(d => d.Id == request.TargetId)
            .Select(d => d.HostName)
            .FirstAsync(cancellationToken);

        var profile = new TestIdentityProfile
        {
            CompanyId = currentUser.CompanyId.Value,
            TargetId = request.TargetId,
            ProfileName = request.ProfileName.Trim(),
            TargetDomain = domain,
            ProgramName = request.ProgramName,
            ProgramUrl = request.ProgramUrl,
            Email = request.Email.Trim(),
            Username = request.Username?.Trim(),
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim(),
            DisplayName = request.DisplayName?.Trim() ?? "Security Test",
            Country = request.Country,
            BirthDate = request.BirthDate,
            OwnershipConfirmed = true,
            TestingPermissionConfirmed = true,
            CreatedBy = currentUser.UserId.Value
        };
        db.TestIdentityProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("authscan.identity_profile.created", "TestIdentityProfile", profile.Id.ToString(),
            new { profile.TargetId, profile.Email }, cancellationToken);
        return Result<Guid>.Success(profile.Id);
    }

    public async Task<Result<RegistrationPlanDto>> PlanRegistrationAsync(
        Guid targetId,
        Guid identityProfileId,
        CancellationToken cancellationToken = default)
    {
        var gate = await EnsureTargetAuthorizedAsync(targetId, cancellationToken);
        if (!gate.IsSuccess)
        {
            return Result<RegistrationPlanDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var scope = await scopeValidator.GetActiveAsync(targetId, cancellationToken);
        if (scope is not null && !scopeValidator.IsTestTypeAllowed(scope, "AUTO_REGISTRATION")
            && scope.ProhibitedTestMethods.Contains("AUTO_REGISTRATION", StringComparison.OrdinalIgnoreCase))
        {
            return Result<RegistrationPlanDto>.Failure(
                "registration_prohibited",
                "Program politikası otomatik hesap oluşturmaya izin vermiyor.");
        }

        var profile = await db.TestIdentityProfiles.FirstOrDefaultAsync(p => p.Id == identityProfileId, cancellationToken);
        if (profile is null || profile.TargetId != targetId)
        {
            return Result<RegistrationPlanDto>.Failure("profile_not_found", "Test identity profile bulunamadı.");
        }

        var regUrl = registrationDetector.CandidatePaths
            .Select(p => $"https://{profile.TargetDomain.TrimEnd('/')}{p}")
            .First();

        // Plan without fetching remote HTML when offline — fields from profile.
        var fields = new List<string> { "Email", "Username", "DisplayName", "Password", "ConfirmPassword" };
        var manual = new List<string>
        {
            "Form doldurulacak ancak gönderilmeyecek — açık onayınız gerekir.",
            "Terms/CAPTCHA/MFA çıkarsa otomasyon durur; atlatılmaz.",
            "Newsletter/pazarlama kutuları işaretlenmez."
        };

        return Result<RegistrationPlanDto>.Success(new RegistrationPlanDto(
            targetId,
            identityProfileId,
            regUrl,
            fields,
            manual,
            ManualTakeoverReason.None,
            "E-posta sistem tarafından uydurulmaz. Yalnızca sizin sağladığınız test kimliği kullanılır. Reward/otomatik submit yok.",
            true));
    }

    public async Task<Result<TargetTestAccountDto>> ConfirmRegistrationSubmitAsync(
        ConfirmRegistrationSubmitRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.ExplicitSubmitApproval)
        {
            return Result<TargetTestAccountDto>.Failure("submit_approval_required", "Form gönderimi için açık onay zorunlu.");
        }

        var gate = await EnsureTargetAuthorizedAsync(request.TargetId, cancellationToken);
        if (!gate.IsSuccess)
        {
            return Result<TargetTestAccountDto>.Failure(gate.ErrorCode!, gate.ErrorMessage!);
        }

        var profile = await db.TestIdentityProfiles.FirstOrDefaultAsync(p => p.Id == request.IdentityProfileId, cancellationToken);
        if (profile is null)
        {
            return Result<TargetTestAccountDto>.Failure("profile_not_found", "Profile yok.");
        }

        var activeCount = await db.TargetTestAccounts.CountAsync(
            a => a.TargetId == request.TargetId && a.IsActive, cancellationToken);
        if (activeCount >= MaxAccountsPerTarget)
        {
            return Result<TargetTestAccountDto>.Failure(
                "max_accounts",
                $"Hedef başına en fazla {MaxAccountsPerTarget} aktif test hesabı.");
        }

        var hasA = await db.TargetTestAccounts.AnyAsync(
            a => a.TargetId == request.TargetId && a.IsActive && a.Role == ValidationSessionRole.TestAccountA,
            cancellationToken);

        var password = vault.GenerateStrongPassword();
        var entity = new TargetTestAccount
        {
            CompanyId = profile.CompanyId,
            TargetId = request.TargetId,
            TargetDomain = profile.TargetDomain,
            Label = hasA ? "Security Test Account 2" : "Security Test Account 1",
            Email = profile.Email,
            Username = profile.Username,
            DisplayName = profile.DisplayName,
            EncryptedSecretReference = vault.ProtectPassword(password),
            AccountStatus = TestAccountStatus.PendingVerification,
            VerificationStatus = TestAccountVerificationStatus.EmailPending,
            RegistrationUrl = request.RegistrationUrl,
            LoginUrl = null,
            OwnershipConfirmed = profile.OwnershipConfirmed,
            TestingPermissionConfirmed = profile.TestingPermissionConfirmed,
            IdentityProfileId = profile.Id,
            CreatedBy = profile.CreatedBy,
            Role = hasA ? ValidationSessionRole.TestAccountB : ValidationSessionRole.TestAccountA,
            IsActive = true,
            Notes = "Registration submit approved; pending verification. Password only in vault."
        };
        db.TargetTestAccounts.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("authscan.registration.submit_approved", "TargetTestAccount", entity.Id.ToString(),
            new { entity.TargetId, entity.RegistrationUrl, Fields = new[] { "Email", "Username", "DisplayName", "Password" } },
            cancellationToken);

        // Password never returned in DTO/logs.
        return Result<TargetTestAccountDto>.Success(Map(entity));
    }

    public async Task<Result> ChangePasswordAsync(
        Guid accountId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 20)
        {
            return Result.Failure("weak_password", "Yeni parola en az 20 karakter olmalıdır.");
        }

        var entity = await db.TargetTestAccounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure("not_found", "Hesap bulunamadı.");
        }

        entity.EncryptedSecretReference = vault.ProtectPassword(newPassword);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("authscan.test_account.password_changed", "TargetTestAccount", accountId.ToString(),
            new { entity.TargetId }, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DisableAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var entity = await db.TargetTestAccounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure("not_found", "Hesap bulunamadı.");
        }

        entity.IsActive = false;
        entity.AccountStatus = TestAccountStatus.Disabled;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("authscan.test_account.disabled", "TargetTestAccount", accountId.ToString(), null, cancellationToken);
        return Result.Success();
    }

    public async Task<Result> DeleteVaultAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var entity = await db.TargetTestAccounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure("not_found", "Hesap bulunamadı.");
        }

        entity.EncryptedSecretReference = vault.ProtectPassword(Guid.NewGuid().ToString("N"));
        entity.IsActive = false;
        entity.AccountStatus = TestAccountStatus.Disabled;
        entity.Notes = (entity.Notes ?? string.Empty) + " | vault wiped";
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("authscan.test_account.vault_wiped", "TargetTestAccount", accountId.ToString(), null, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<string>> RevealPasswordAsync(
        Guid accountId,
        bool forCopy,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsSystemAdmin && !currentUser.IsInRole("CompanyAdmin"))
        {
            return Result<string>.Failure("forbidden", "Secret görüntüleme yetkisi yok.");
        }

        var entity = await db.TargetTestAccounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        if (entity is null)
        {
            return Result<string>.Failure("not_found", "Hesap bulunamadı.");
        }

        await audit.WriteAsync(
            forCopy ? "authscan.test_account.secret_copied" : "authscan.test_account.secret_revealed",
            "TargetTestAccount",
            accountId.ToString(),
            new { entity.TargetId },
            cancellationToken);

        try
        {
            return Result<string>.Success(vault.UnprotectPassword(entity.EncryptedSecretReference));
        }
        catch
        {
            return Result<string>.Failure("vault_error", "Secret çözülemedi.");
        }
    }

    private async Task<Result> EnsureTargetAuthorizedAsync(Guid targetId, CancellationToken cancellationToken)
    {
        var mock = env.IsDevelopment();
        var scope = await scopeValidator.GetActiveAsync(targetId, cancellationToken);
        var auth = await authEvidence.GetActiveAsync(targetId, cancellationToken);
        if (!mock && scope is null)
        {
            return Result.Failure("scope_required", "Scope doğrulanmadan hesap oluşturulamıyor.");
        }

        if (!mock && auth is null)
        {
            return Result.Failure("authorization_required", "AuthorizationEvidence olmadan hesap oluşturulamıyor.");
        }

        return Result.Success();
    }

    private static TargetTestAccountDto Map(TargetTestAccount a) =>
        new(a.Id, a.TargetId, a.TargetDomain, a.Label, a.Email, a.Username, a.DisplayName,
            a.AccountStatus, a.VerificationStatus, a.RegistrationUrl, a.LoginUrl,
            a.LastSuccessfulLoginAt, a.LastAuthenticatedScanAt, a.OwnershipConfirmed,
            a.TestingPermissionConfirmed, a.IsActive, a.Role, a.Notes);
}
