using System.Security.Cryptography;
using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Application.Common.Exceptions;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Domains;
using Kaan.SecurityPlatform.Application.Features.Domains.Dtos;
using Kaan.SecurityPlatform.Domain.Entities.Projects;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace Kaan.SecurityPlatform.Infrastructure.Domains;

public sealed class DomainAssetService : IDomainAssetService
{
    private readonly IApplicationDbContext _db;
    private readonly IDomainVerificationService _verification;
    private readonly ITargetSafetyValidator _safety;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogWriter _audit;
    private readonly IActivityEventPublisher _activity;

    public DomainAssetService(
        IApplicationDbContext db,
        IDomainVerificationService verification,
        ITargetSafetyValidator safety,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IAuditLogWriter audit,
        IActivityEventPublisher activity)
    {
        _db = db;
        _verification = verification;
        _safety = safety;
        _currentUser = currentUser;
        _clock = clock;
        _audit = audit;
        _activity = activity;
    }

    public async Task<IReadOnlyList<DomainListItemDto>> ListAsync(Guid? projectId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.DomainAssets.AsQueryable();
        if (projectId is Guid pid)
        {
            query = query.Where(d => d.SecurityProjectId == pid);
        }

        return await query
            .OrderByDescending(d => d.Source == "HackerOne")
            .ThenByDescending(d => d.HackerOneEligibleForBounty == true)
            .ThenByDescending(d => d.CreatedAt)
            .Select(d => new DomainListItemDto(
                d.Id,
                d.SecurityProjectId,
                d.HostName,
                d.Scheme,
                d.IsVerified,
                d.Status,
                d.VerifiedAt,
                d.CreatedAt,
                d.Source,
                d.HackerOneProgramHandle,
                d.HackerOneProgramName,
                d.HackerOneEligibleForBounty,
                d.HackerOneOffersBounties,
                d.HackerOneCurrency,
                d.HackerOneMaxSeverity,
                d.HackerOneBountySummary,
                d.HackerOneIsWildcard,
                d.HackerOneAssetType))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<DomainDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var domain = await _db.DomainAssets.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (domain is null)
        {
            return Result<DomainDetailDto>.Failure("domain_not_found", "Domain bulunamadı.");
        }
        return Result<DomainDetailDto>.Success(Map(domain));
    }

    public async Task<Result<DomainDetailDto>> CreateAsync(CreateDomainRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = await CompanyIdResolver.ResolveAsync(_currentUser, _db, cancellationToken);
        var normalized = NormalizeHost(request.HostName);

        var uri = new Uri($"{request.Scheme}://{normalized}{(request.Port is int port ? ":" + port : "")}/");
        var check = _safety.ValidateUri(uri);
        if (!check.IsSafe)
        {
            return Result<DomainDetailDto>.Failure(check.ReasonCode ?? "unsafe_target", check.Detail ?? "Bu domain taranamaz.");
        }

        var project = await _db.SecurityProjects.FirstOrDefaultAsync(p => p.Id == request.SecurityProjectId, cancellationToken);
        if (project is null)
        {
            return Result<DomainDetailDto>.Failure("project_not_found", "Proje bulunamadı.");
        }

        var duplicate = await _db.DomainAssets.AnyAsync(
            d => d.SecurityProjectId == request.SecurityProjectId
              && d.NormalizedHostName == normalized
              && d.HackerOneProgramHandle == null,
            cancellationToken);
        if (duplicate)
        {
            return Result<DomainDetailDto>.Failure("domain_already_exists", "Bu domain proje içinde zaten kayıtlı.");
        }

        var entity = new DomainAsset
        {
            CompanyId = companyId,
            SecurityProjectId = request.SecurityProjectId,
            HostName = request.HostName,
            NormalizedHostName = normalized,
            Scheme = request.Scheme,
            Port = request.Port,
            Source = "Manual",
            Status = DomainAssetStatus.PendingVerification,
            IsVerified = false,
            CreatedAt = _clock.UtcNow
        };

        _db.DomainAssets.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync("domain.create", "DomainAsset", entity.Id.ToString(), new { hostName = entity.HostName }, cancellationToken: cancellationToken);

        return Result<DomainDetailDto>.Success(Map(entity));
    }

    public async Task<Result<StartVerificationResponse>> StartVerificationAsync(StartVerificationRequest request, CancellationToken cancellationToken = default)
    {
        var domain = await _db.DomainAssets.FirstOrDefaultAsync(d => d.Id == request.DomainId, cancellationToken);
        if (domain is null)
        {
            return Result<StartVerificationResponse>.Failure("domain_not_found", "Domain bulunamadı.");
        }

        if (string.IsNullOrEmpty(domain.VerificationToken))
        {
            domain.VerificationToken = GenerateToken();
            domain.VerificationTokenCreatedAt = _clock.UtcNow;
        }
        domain.VerificationMethod = request.Method;
        await _db.SaveChangesAsync(cancellationToken);

        var instruction = request.Method switch
        {
            VerificationMethod.DnsTxt => $"_kaan-security.{domain.NormalizedHostName} altında TXT kaydı oluşturun ve değerine tam olarak '{domain.VerificationToken}' yazın.",
            VerificationMethod.HtmlFile => $"'https://{domain.NormalizedHostName}/.well-known/kaan-security-verification.txt' adresinden erişilebilecek şekilde içeriği '{domain.VerificationToken}' olan bir dosya yayınlayın.",
            VerificationMethod.MetaTag => $"Ana sayfanızın <head> bölümüne <meta name=\"kaan-security-verification\" content=\"{domain.VerificationToken}\" /> etiketini ekleyin.",
            VerificationMethod.Mock => $"Geliştirme ortamı: token'ı '{domain.VerificationToken}' olarak kullanın.",
            _ => "Bu doğrulama yöntemi desteklenmiyor."
        };

        return Result<StartVerificationResponse>.Success(new StartVerificationResponse(
            domain.Id,
            request.Method,
            domain.VerificationToken!,
            instruction));
    }

    public async Task<Result<RunVerificationResponse>> RunVerificationAsync(Guid domainId, CancellationToken cancellationToken = default)
    {
        var domain = await _db.DomainAssets.FirstOrDefaultAsync(d => d.Id == domainId, cancellationToken);
        if (domain is null)
        {
            return Result<RunVerificationResponse>.Failure("domain_not_found", "Domain bulunamadı.");
        }

        if (string.IsNullOrEmpty(domain.VerificationToken) || domain.VerificationMethod is null)
        {
            throw new DomainNotVerifiedException(domain.HostName);
        }

        var outcome = await _verification.VerifyAsync(
            domain.NormalizedHostName,
            domain.VerificationToken,
            domain.VerificationMethod.Value,
            cancellationToken);

        if (outcome.IsVerified)
        {
            domain.IsVerified = true;
            domain.Status = DomainAssetStatus.Verified;
            domain.VerifiedAt = _clock.UtcNow;
            domain.LastVerificationError = null;
        }
        else
        {
            domain.LastVerificationError = outcome.ErrorDetail;
            if (domain.Status == DomainAssetStatus.PendingVerification)
            {
                domain.Status = DomainAssetStatus.Failed;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("domain.verify", "DomainAsset", domain.Id.ToString(),
            new { outcome.IsVerified, outcome.Method, outcome.ErrorCode }, cancellationToken: cancellationToken);

        if (outcome.IsVerified)
        {
            await _activity.PublishToCompanyAsync(domain.CompanyId, "domain.verified", new { domainId = domain.Id, host = domain.HostName }, cancellationToken);
        }

        return Result<RunVerificationResponse>.Success(new RunVerificationResponse(
            domain.Id,
            outcome.IsVerified,
            outcome.Method,
            outcome.Evidence,
            outcome.ErrorCode,
            outcome.ErrorDetail));
    }

    public async Task<Result<SetVerificationManualResponse>> SetVerificationManualAsync(
        SetVerificationManualRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsInRole(Roles.SystemAdmin))
        {
            return Result<SetVerificationManualResponse>.Failure("forbidden", "Yalnızca SystemAdmin manuel doğrulama yapabilir.");
        }

        var domain = await _db.DomainAssets.FirstOrDefaultAsync(d => d.Id == request.DomainId, cancellationToken);
        if (domain is null)
        {
            return Result<SetVerificationManualResponse>.Failure("domain_not_found", "Domain bulunamadı.");
        }

        if (domain.Status == DomainAssetStatus.Archived)
        {
            return Result<SetVerificationManualResponse>.Failure(
                "domain_archived",
                "Arşivlenmiş domainin doğrulama durumu değiştirilemez.");
        }

        if (request.IsVerified)
        {
            domain.IsVerified = true;
            domain.Status = DomainAssetStatus.Verified;
            domain.VerifiedAt = _clock.UtcNow;
            domain.LastVerificationError = null;
            domain.VerificationMethod ??= VerificationMethod.Mock;
        }
        else
        {
            domain.IsVerified = false;
            domain.Status = DomainAssetStatus.PendingVerification;
            domain.VerifiedAt = null;
            domain.LastVerificationError = request.Note;
        }

        domain.UpdatedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync(
            "domain.verify.manual",
            "DomainAsset",
            domain.Id.ToString(),
            new { request.IsVerified, request.Note, host = domain.HostName },
            cancellationToken: cancellationToken);

        if (request.IsVerified)
        {
            await _activity.PublishToCompanyAsync(
                domain.CompanyId,
                "domain.verified",
                new { domainId = domain.Id, host = domain.HostName, manual = true },
                cancellationToken);
            await _activity.PublishToSystemAdminsAsync(
                "domain.verified",
                new { domainId = domain.Id, host = domain.HostName, manual = true },
                cancellationToken);
        }

        return Result<SetVerificationManualResponse>.Success(new SetVerificationManualResponse(
            domain.Id,
            domain.IsVerified,
            domain.Status,
            domain.VerifiedAt));
    }

    public async Task<Result> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var domain = await _db.DomainAssets.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (domain is null)
        {
            return Result.Failure("domain_not_found", "Domain bulunamadı.");
        }

        domain.Status = DomainAssetStatus.Archived;
        domain.IsVerified = false;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static string NormalizeHost(string host)
    {
        var normalized = host.Trim().TrimEnd('.').ToLowerInvariant();
        if (normalized.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[7..];
        }
        else if (normalized.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[8..];
        }
        return normalized.Split('/')[0];
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return "kaan-" + Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static DomainDetailDto Map(DomainAsset d) => new(
        d.Id, d.CompanyId, d.SecurityProjectId,
        d.HostName, d.NormalizedHostName, d.Scheme, d.Port,
        d.IsVerified, d.Status, d.VerificationMethod, d.VerificationToken,
        d.VerifiedAt, d.LastVerificationError,
        d.CreatedAt, d.UpdatedAt);
}
