using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Scans;
using Kaan.SecurityPlatform.Application.Features.Scans.Dtos;
using Kaan.SecurityPlatform.Domain.Entities.Scans;
using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning;

public sealed class ScanService : IScanService
{
    private readonly IApplicationDbContext _db;
    private readonly IScanQueue _queue;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IActivityEventPublisher _activity;
    private readonly IAuditLogWriter _audit;
    private readonly IAssessmentModeGuard _modeGuard;
    private readonly IHostEnvironment _environment;

    public ScanService(
        IApplicationDbContext db,
        IScanQueue queue,
        ICurrentUser currentUser,
        IDateTimeProvider clock,
        IActivityEventPublisher activity,
        IAuditLogWriter audit,
        IAssessmentModeGuard modeGuard,
        IHostEnvironment environment)
    {
        _db = db;
        _queue = queue;
        _currentUser = currentUser;
        _clock = clock;
        _activity = activity;
        _audit = audit;
        _modeGuard = modeGuard;
        _environment = environment;
    }

    public async Task<Result<StartScanResponse>> StartAsync(StartScanRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            return Result<StartScanResponse>.Failure("unauthorized", "Bu işlem için kimliğinizin doğrulanması gerekiyor.");
        }

        if (!_currentUser.IsSystemAdmin)
        {
            return Result<StartScanResponse>.Failure(
                "system_admin_required",
                "Tarama başlatma yalnızca SystemAdmin onayıyla yapılabilir.");
        }

        var nameCheck = _modeGuard.EnsureNameAllowed(request.AssessmentModeName);
        if (nameCheck.IsFailure)
        {
            return Result<StartScanResponse>.Failure(nameCheck.ErrorCode!, nameCheck.ErrorMessage!);
        }

        var mode = request.AssessmentMode;
        if (!string.IsNullOrWhiteSpace(request.AssessmentModeName) &&
            Enum.TryParse<AssessmentMode>(request.AssessmentModeName, ignoreCase: true, out var parsed))
        {
            mode = parsed;
        }

        var modeCheck = _modeGuard.EnsureEnvironmentAllows(mode, _environment.EnvironmentName);
        if (modeCheck.IsFailure)
        {
            return Result<StartScanResponse>.Failure(modeCheck.ErrorCode!, modeCheck.ErrorMessage!);
        }

        if (mode == AssessmentMode.IsolatedSecurityLab)
        {
            return Result<StartScanResponse>.Failure(
                "wrong_assessment_mode",
                "IsolatedSecurityLab için /api/admin/lab kullanın.");
        }

        if (mode is not (
            AssessmentMode.PublicPassiveAssessment
            or AssessmentMode.AuthorizedExternalAssessment
            or AssessmentMode.ApplicationSecurityCandidate))
        {
            return Result<StartScanResponse>.Failure(
                "wrong_assessment_mode",
                "Bu endpoint PublicPassiveAssessment, AuthorizedExternalAssessment veya ApplicationSecurityCandidate içindir.");
        }

        var domain = await _db.DomainAssets.FirstOrDefaultAsync(d => d.Id == request.DomainAssetId, cancellationToken);
        if (domain is null)
        {
            return Result<StartScanResponse>.Failure("domain_not_found", "Domain bulunamadı.");
        }

        // PublicPassive: doğrulama gerekmez. AuthorizedExternal / ApplicationSecurityCandidate: doğrulanmış domain zorunlu.
        if (mode is AssessmentMode.AuthorizedExternalAssessment or AssessmentMode.ApplicationSecurityCandidate
            && !domain.IsVerified)
        {
            return Result<StartScanResponse>.Failure(
                "domain_not_verified",
                $"{mode} yalnızca doğrulanmış domainlerde başlatılabilir.");
        }

        var job = new ScanJob
        {
            CompanyId = domain.CompanyId,
            SecurityProjectId = domain.SecurityProjectId,
            DomainAssetId = domain.Id,
            ScanType = request.ScanType,
            AssessmentMode = mode,
            Status = ScanStatus.Queued,
            RequestedByUserId = userId,
            CreatedAt = _clock.UtcNow
        };

        _db.ScanJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);

        var queueId = await _queue.EnqueueScanAsync(job.Id, cancellationToken);
        job.HangfireJobId = queueId;
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync("scan.start", "ScanJob", job.Id.ToString(),
            new
            {
                domainId = domain.Id,
                host = domain.HostName,
                scanType = request.ScanType.ToString(),
                assessmentMode = mode.ToString()
            },
            category: "scan", cancellationToken: cancellationToken);

        var queuedPayload = new
        {
            scanJobId = job.Id,
            host = domain.HostName,
            scanType = job.ScanType.ToString(),
            assessmentMode = job.AssessmentMode.ToString()
        };
        await _activity.PublishToCompanyAsync(job.CompanyId, "scan.queued", queuedPayload, cancellationToken);
        await _activity.PublishToUserAsync(userId, "scan.queued", queuedPayload, cancellationToken);
        await _activity.PublishToSystemAdminsAsync("scan.queued", queuedPayload, cancellationToken);

        return Result<StartScanResponse>.Success(new StartScanResponse(job.Id, job.Status, queueId));
    }

    public async Task<IReadOnlyList<ScanJobListItemDto>> ListAsync(Guid? projectId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.ScanJobs.AsQueryable();
        if (projectId is Guid pid)
        {
            query = query.Where(j => j.SecurityProjectId == pid);
        }

        return await query
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => new ScanJobListItemDto(
                j.Id,
                j.SecurityProjectId,
                j.DomainAssetId,
                j.DomainAsset!.HostName,
                j.ScanType,
                j.AssessmentMode,
                j.Status,
                j.StartedAt,
                j.CompletedAt,
                j.ProgressPercentage,
                j.CurrentStep,
                j.Result != null ? j.Result.SecurityScore : (int?)null))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<ScanJobDetailDto>> GetAsync(Guid scanJobId, CancellationToken cancellationToken = default)
    {
        var job = await _db.ScanJobs
            .Include(j => j.DomainAsset)
            .Include(j => j.Result)
            .FirstOrDefaultAsync(j => j.Id == scanJobId, cancellationToken);
        if (job is null)
        {
            return Result<ScanJobDetailDto>.Failure("scan_not_found", "Tarama işi bulunamadı.");
        }

        return Result<ScanJobDetailDto>.Success(new ScanJobDetailDto(
            job.Id,
            job.SecurityProjectId,
            job.DomainAssetId,
            job.DomainAsset?.HostName ?? string.Empty,
            job.ScanType,
            job.AssessmentMode,
            job.Status,
            job.StartedAt,
            job.CompletedAt,
            job.ProgressPercentage,
            job.CurrentStep,
            job.TotalSteps,
            job.CompletedSteps,
            job.ErrorMessage,
            job.IsRetest,
            job.Result is null ? null : new ScanResultDto(
                job.Result.Id,
                job.Result.SecurityScore,
                job.Result.PreviousSecurityScore,
                job.Result.CriticalCount,
                job.Result.HighCount,
                job.Result.MediumCount,
                job.Result.LowCount,
                job.Result.InfoCount,
                job.Result.ExecutiveSummary,
                job.Result.Summary,
                job.Result.ChecksTotal,
                job.Result.ChecksPassed,
                job.Result.ChecksFailed)));
    }

    public async Task<Result<ScanProgressDto>> GetProgressAsync(Guid scanJobId, CancellationToken cancellationToken = default)
    {
        var job = await _db.ScanJobs
            .Where(j => j.Id == scanJobId)
            .Select(j => new ScanProgressDto(j.Id, j.Status, j.ProgressPercentage, j.CurrentStep, j.CompletedSteps, j.TotalSteps))
            .FirstOrDefaultAsync(cancellationToken);
        if (job is null)
        {
            return Result<ScanProgressDto>.Failure("scan_not_found", "Tarama işi bulunamadı.");
        }
        return Result<ScanProgressDto>.Success(job);
    }

    public async Task<Result<StartScanResponse>> RetestFindingAsync(RetestRequest request, CancellationToken cancellationToken = default)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            return Result<StartScanResponse>.Failure("unauthorized", "Bu işlem için kimliğinizin doğrulanması gerekiyor.");
        }

        var finding = await _db.Findings
            .Include(f => f.ScanResult)
            .ThenInclude(r => r!.ScanJob)
            .ThenInclude(j => j!.DomainAsset)
            .FirstOrDefaultAsync(f => f.Id == request.FindingId, cancellationToken);
        if (finding is null || finding.ScanResult?.ScanJob?.DomainAsset is null)
        {
            return Result<StartScanResponse>.Failure("finding_not_found", "Bulgu bulunamadı.");
        }

        if (!_currentUser.IsSystemAdmin)
        {
            return Result<StartScanResponse>.Failure(
                "system_admin_required",
                "Yeniden test yalnızca SystemAdmin onayıyla başlatılabilir.");
        }

        var domain = finding.ScanResult.ScanJob.DomainAsset;

        var newJob = new ScanJob
        {
            CompanyId = domain.CompanyId,
            SecurityProjectId = domain.SecurityProjectId,
            DomainAssetId = domain.Id,
            ScanType = finding.ScanResult.ScanJob.ScanType,
            AssessmentMode = AssessmentMode.PublicPassiveAssessment,
            Status = ScanStatus.Queued,
            RequestedByUserId = userId,
            IsRetest = true,
            RetestForFindingId = finding.Id,
            PreviousScanJobId = finding.ScanResult.ScanJobId,
            CreatedAt = _clock.UtcNow
        };

        _db.ScanJobs.Add(newJob);
        await _db.SaveChangesAsync(cancellationToken);

        var queueId = await _queue.EnqueueScanAsync(newJob.Id, cancellationToken);
        newJob.HangfireJobId = queueId;
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.WriteAsync("scan.retest", "ScanJob", newJob.Id.ToString(),
            new { findingId = finding.Id, host = domain.HostName },
            category: "scan", cancellationToken: cancellationToken);

        return Result<StartScanResponse>.Success(new StartScanResponse(newJob.Id, newJob.Status, queueId));
    }
}
