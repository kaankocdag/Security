using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Validation;
using Kaan.SecurityPlatform.Application.Features.Validation.Dtos;
using Kaan.SecurityPlatform.Domain.Entities.Validation;
using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kaan.SecurityPlatform.Infrastructure.Validation;

public sealed class ValidationCatalogService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    ITestAccountSecretProtector secretProtector) : IValidationCatalogService
{
    public async Task<Result<ScopePolicy>> UpsertScopeAsync(
        UpsertScopePolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.CompanyId is null)
        {
            return Result<ScopePolicy>.Failure("no_tenant", "Şirket bağlamı yok.");
        }

        var entity = await db.ScopePolicies
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(p => p.TargetId == request.TargetId, cancellationToken);
        if (entity is null)
        {
            entity = new ScopePolicy
            {
                CompanyId = currentUser.CompanyId.Value,
                TargetId = request.TargetId
            };
            db.ScopePolicies.Add(entity);
        }

        entity.ProgramName = request.ProgramName.Trim();
        entity.ProgramUrl = request.ProgramUrl;
        entity.ScopeStatus = request.ScopeStatus;
        entity.AllowedTestMethods = request.AllowedTestMethods;
        entity.ProhibitedTestMethods = request.ProhibitedTestMethods;
        entity.RateLimit = Math.Clamp(request.RateLimit, 1, 10);
        entity.ValidFrom = request.ValidFrom;
        entity.ValidUntil = request.ValidUntil;
        entity.TargetInBountyScope = request.TargetInBountyScope;
        entity.PolicyEvidence = request.PolicyEvidence;
        entity.LastVerifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Result<ScopePolicy>.Success(entity);
    }

    public async Task<Result<Guid>> UpsertTestAccountAsync(
        UpsertTestAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.CompanyId is null || currentUser.UserId is null)
        {
            return Result<Guid>.Failure("no_tenant", "Şirket/kullanıcı bağlamı yok.");
        }

        if (!request.OwnershipConfirmed || !request.TestingPermissionConfirmed)
        {
            return Result<Guid>.Failure(
                "ownership_required",
                "Test hesabı için OwnershipConfirmed ve TestingPermissionConfirmed zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(request.SecretMaterial))
        {
            return Result<Guid>.Failure("secret_required", "Secret material zorunlu (şifreli saklanır).");
        }

        var entity = await db.TestAccountSessions.FirstOrDefaultAsync(
            a => a.TargetId == request.TargetId && a.Role == request.Role, cancellationToken);
        if (entity is null)
        {
            entity = new TestAccountSession
            {
                CompanyId = currentUser.CompanyId.Value,
                TargetId = request.TargetId,
                Role = request.Role,
                CreatedBy = currentUser.UserId.Value
            };
            db.TestAccountSessions.Add(entity);
        }

        entity.Label = request.Label.Trim();
        entity.OwnershipConfirmed = request.OwnershipConfirmed;
        entity.TestingPermissionConfirmed = request.TestingPermissionConfirmed;
        entity.EncryptedSecretReference = secretProtector.Protect(request.SecretMaterial);
        entity.OwnedTestResourceHint = request.OwnedTestResourceHint;
        entity.LastUsedAt = null;
        await db.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(entity.Id);
    }
}
