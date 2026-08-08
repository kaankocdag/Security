using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Findings;
using Kaan.SecurityPlatform.Application.Features.Findings.Dtos;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Microsoft.EntityFrameworkCore;

namespace Kaan.SecurityPlatform.Infrastructure.Findings;

public sealed class FindingService : IFindingService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public FindingService(IApplicationDbContext db, ICurrentUser currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<FindingListItemDto>> ListAsync(Guid? scanResultId = null, Guid? projectId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Findings.AsQueryable();
        if (scanResultId is Guid rid)
        {
            query = query.Where(f => f.ScanResultId == rid);
        }
        if (projectId is Guid pid)
        {
            query = query.Where(f => f.ScanResult!.ScanJob!.SecurityProjectId == pid);
        }

        return await query
            .OrderByDescending(f => f.TechnicalSeverity)
            .ThenByDescending(f => f.LastSeenAt)
            .Select(f => new FindingListItemDto(
                f.Id,
                f.ScanResultId,
                f.ScanResult != null ? f.ScanResult.ScanJobId : (Guid?)null,
                f.ScanResult != null && f.ScanResult.ScanJob != null && f.ScanResult.ScanJob.DomainAsset != null
                    ? f.ScanResult.ScanJob.DomainAsset.HostName
                    : null,
                f.Title,
                f.Severity,
                f.TechnicalSeverity,
                f.FindingClass,
                f.BugBountyEligible,
                f.SubmissionRecommendation,
                f.ConfidenceLevel,
                f.Category,
                f.Status,
                f.AffectedUrl,
                f.Fingerprint,
                f.FirstSeenAt,
                f.LastSeenAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<FindingDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var finding = await _db.Findings
            .Include(f => f.KnowledgeLinks).ThenInclude(l => l.Article)
            .Include(f => f.ScanResult!).ThenInclude(r => r.ScanJob!).ThenInclude(j => j.DomainAsset)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (finding is null)
        {
            return Result<FindingDetailDto>.Failure("finding_not_found", "Bulgu bulunamadı.");
        }

        var dto = new FindingDetailDto(
            finding.Id,
            finding.CompanyId,
            finding.ScanResultId,
            finding.ScanResult?.ScanJobId,
            finding.ScanResult?.ScanJob?.DomainAssetId,
            finding.ScanResult?.ScanJob?.DomainAsset?.HostName,
            finding.ScanResult?.ScanJob?.DomainAsset?.IsVerified == true,
            finding.Title,
            finding.Description,
            finding.TechnicalDescription,
            finding.BusinessImpact,
            finding.Severity,
            finding.TechnicalSeverity,
            finding.Exploitability,
            finding.DemonstratedImpact,
            finding.RequiresManualValidation,
            finding.FindingClass,
            finding.BugBountyEligible,
            finding.EligibilityReason,
            finding.ProgramPolicyMatch,
            finding.SubmissionRecommendation,
            finding.PolicyCategory,
            finding.ConfidenceLevel,
            finding.Category,
            finding.CweCode,
            finding.OwaspCategory,
            finding.AffectedUrl,
            finding.AffectedParameter,
            finding.Evidence,
            finding.Remediation,
            finding.RemediationExampleConfig,
            finding.TurkishExecutiveSummary,
            finding.Status,
            finding.CheckCode,
            finding.Fingerprint,
            finding.FirstSeenAt,
            finding.LastSeenAt,
            finding.ConfirmedVulnerability,
            finding.LatestValidationStatus,
            finding.SubmissionEligible,
            finding.PotentialRewardEligible,
            finding.LatestValidationRunId,
            finding.KnowledgeLinks
                .Where(l => l.Article is not null)
                .Select(l => new FindingKnowledgeLinkDto(l.ArticleId, l.Article!.Slug, l.Article.Title, l.RelevanceScore))
                .ToList());

        return Result<FindingDetailDto>.Success(dto);
    }

    public async Task<Result> UpdateStatusAsync(Guid id, UpdateFindingStatusRequest request, CancellationToken cancellationToken = default)
    {
        var finding = await _db.Findings.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (finding is null)
        {
            return Result.Failure("finding_not_found", "Bulgu bulunamadı.");
        }

        if (_currentUser.UserId is not Guid userId)
        {
            return Result.Failure("unauthorized", "Bu işlem için kimliğinizin doğrulanması gerekiyor.");
        }

        var previous = finding.Status;
        finding.Status = request.NewStatus;
        finding.UpdatedAt = _clock.UtcNow;

        _db.FindingStatusHistories.Add(new FindingStatusHistory
        {
            CompanyId = finding.CompanyId,
            FindingId = finding.Id,
            PreviousStatus = previous,
            NewStatus = request.NewStatus,
            ChangedByUserId = userId,
            Note = request.Note,
            CreatedAt = _clock.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
