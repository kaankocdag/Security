using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Features.BugBounty;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Entities.Notifications;
using Kaan.SecurityPlatform.Domain.Entities.Scans;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.BugBounty;
using Kaan.SecurityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Executor;

public sealed class PassiveScanExecutor : IScanExecutor
{
    private readonly SecurityPlatformDbContext _dbContext;
    private readonly IEnumerable<IPassiveSecurityCheck> _checks;
    private readonly ISecurityScoreCalculator _scoreCalculator;
    private readonly IFindingValidationClassifier _classifier;
    private readonly IActivityEventPublisher _activity;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<PassiveScanExecutor> _logger;

    public PassiveScanExecutor(
        SecurityPlatformDbContext dbContext,
        IEnumerable<IPassiveSecurityCheck> checks,
        ISecurityScoreCalculator scoreCalculator,
        IFindingValidationClassifier classifier,
        IActivityEventPublisher activity,
        IDateTimeProvider clock,
        ILogger<PassiveScanExecutor> logger)
    {
        _dbContext = dbContext;
        _checks = checks;
        _scoreCalculator = scoreCalculator;
        _classifier = classifier;
        _activity = activity;
        _clock = clock;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid scanJobId, CancellationToken cancellationToken = default)
    {
        // Aynı iş iki Hangfire worker'da koşmasın: tamamlanmış/yeni başlamış taramayı ezmesin.
        var now = _clock.UtcNow;
        var staleBefore = now.AddMinutes(-5);
        var claimed = await _dbContext.ScanJobs
            .IgnoreQueryFilters()
            .Where(j => j.Id == scanJobId && (
                j.Status == ScanStatus.Queued ||
                j.Status == ScanStatus.Failed ||
                (j.Status == ScanStatus.Running && j.StartedAt != null && j.StartedAt < staleBefore)))
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(j => j.Status, ScanStatus.Running)
                    .SetProperty(j => j.StartedAt, now)
                    .SetProperty(j => j.CompletedAt, (DateTime?)null)
                    .SetProperty(j => j.ProgressPercentage, 0)
                    .SetProperty(j => j.CompletedSteps, 0)
                    .SetProperty(j => j.CurrentStep, (string?)null)
                    .SetProperty(j => j.ErrorMessage, (string?)null),
                cancellationToken);

        if (claimed == 0)
        {
            _logger.LogInformation(
                "Tarama atlandı (zaten tamamlandı veya başka worker işliyor): {ScanJobId}",
                scanJobId);
            return;
        }

        var job = await _dbContext.ScanJobs
            .IgnoreQueryFilters()
            .Include(j => j.DomainAsset)
            .Include(j => j.SecurityProject)
            .FirstOrDefaultAsync(j => j.Id == scanJobId, cancellationToken);

        if (job is null)
        {
            _logger.LogWarning("Tarama işi bulunamadı: {ScanJobId}", scanJobId);
            return;
        }

        if (job.DomainAsset is null)
        {
            job.Status = ScanStatus.Failed;
            job.ErrorMessage = "Hedef domain bulunamadı.";
            job.CompletedAt = _clock.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        // PublicPassive: doğrulama gerekmez. AuthorizedExternal: ScanService doğrulamayı zorunlu kılar.
        // Serbest exploit/payload yoktur — mevcut güvenlik kontrol paketi çalışır.
        if (job.AssessmentMode is not (
            AssessmentMode.PublicPassiveAssessment or AssessmentMode.AuthorizedExternalAssessment))
        {
            job.Status = ScanStatus.Failed;
            job.ErrorMessage =
                "Desteklenmeyen değerlendirme modu. Bu executor PublicPassiveAssessment veya AuthorizedExternalAssessment çalıştırır.";
            job.CompletedAt = _clock.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (job.AssessmentMode == AssessmentMode.AuthorizedExternalAssessment && !job.DomainAsset.IsVerified)
        {
            job.Status = ScanStatus.Failed;
            job.ErrorMessage = "AuthorizedExternalAssessment için domain doğrulanmış olmalıdır.";
            job.CompletedAt = _clock.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        job.TotalSteps = _checks.Count();
        await _dbContext.SaveChangesAsync(cancellationToken);
        await PublishProgressAsync(job);

        var scheme = string.IsNullOrEmpty(job.DomainAsset.Scheme) ? "https" : job.DomainAsset.Scheme;
        var uri = new Uri($"{scheme}://{job.DomainAsset.NormalizedHostName}/");

        var findings = new List<Finding>();
        var context = new ScanContext
        {
            ScanJobId = job.Id,
            CompanyId = job.CompanyId,
            TargetUri = uri,
            NormalizedHostName = job.DomainAsset.NormalizedHostName
        };

        var checksTotal = 0;
        var checksPassed = 0;
        var checksFailed = 0;
        var checksSkipped = 0;

        foreach (var check in _checks.OrderBy(c => c.Order))
        {
            cancellationToken.ThrowIfCancellationRequested();
            checksTotal++;
            job.CurrentStep = check.DisplayName;
            // Adım başladı — yüzde henüz artmaz (takılı görünmesin diye tamamlanınca artar)
            var startedPct = Math.Min(99, job.CompletedSteps * 100 / Math.Max(job.TotalSteps, 1));
            job.ProgressPercentage = startedPct;
            await _dbContext.SaveChangesAsync(cancellationToken);
            await PublishProgressAsync(job);

            try
            {
                using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                stepCts.CancelAfter(TimeSpan.FromSeconds(25));
                var outcome = await check.RunAsync(context, stepCts.Token);
                switch (outcome.Status)
                {
                    case CheckStatus.Passed: checksPassed++; break;
                    case CheckStatus.IssuesFound: checksFailed++; break;
                    case CheckStatus.Skipped: checksSkipped++; break;
                    case CheckStatus.Failed: checksFailed++; break;
                }

                foreach (var f in outcome.Findings)
                {
                    var entity = new Finding
                    {
                        CompanyId = job.CompanyId,
                        Title = f.Title,
                        Description = f.Description,
                        TechnicalDescription = f.TechnicalDescription,
                        BusinessImpact = f.BusinessImpact,
                        Severity = f.Severity, // scanner severity — değişmez
                        ConfidenceLevel = f.Confidence,
                        Category = f.Category,
                        CweCode = f.CweCode,
                        OwaspCategory = f.OwaspCategory,
                        AffectedUrl = f.AffectedUrl,
                        AffectedParameter = f.AffectedParameter,
                        Evidence = f.Evidence,
                        Remediation = f.Remediation,
                        RemediationExampleConfig = f.RemediationExampleConfig,
                        TurkishExecutiveSummary = f.TurkishExecutiveSummary,
                        CheckCode = outcome.CheckCode,
                        Fingerprint = f.Fingerprint,
                        FirstSeenAt = _clock.UtcNow,
                        LastSeenAt = _clock.UtcNow
                    };
                    _classifier.Classify(entity, AmazonVrpPolicy.PolicyKeyConstant);
                    findings.Add(entity);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Kontrol zaman aşımı: {Check}", check.CheckCode);
                checksSkipped++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kontrol çalıştırılırken hata: {Check}", check.CheckCode);
                checksFailed++;
            }

            job.CompletedSteps++;
            job.ProgressPercentage = Math.Min(100, job.CompletedSteps * 100 / Math.Max(job.TotalSteps, 1));
            await _dbContext.SaveChangesAsync(cancellationToken);
            await PublishProgressAsync(job);
        }

        var scoreResult = _scoreCalculator.Calculate(findings);

        int previousScore = 0;
        ScanResult? previousResult = null;
        if (job.IsRetest && job.PreviousScanJobId is Guid prevJobId)
        {
            previousResult = await _dbContext.ScanResults
                .IgnoreQueryFilters()
                .Include(r => r.Findings)
                .FirstOrDefaultAsync(r => r.ScanJobId == prevJobId, cancellationToken);
            if (previousResult is not null)
            {
                previousScore = previousResult.SecurityScore;
            }
        }

        var result = new ScanResult
        {
            CompanyId = job.CompanyId,
            ScanJobId = job.Id,
            SecurityScore = scoreResult.Score,
            PreviousSecurityScore = previousScore,
            StartedAt = job.StartedAt ?? _clock.UtcNow,
            CompletedAt = _clock.UtcNow,
            Summary = $"{findings.Count} bulgu bulundu. Puan: {scoreResult.Score}/100 ({scoreResult.Grade}).",
            ExecutiveSummary = scoreResult.ExplanationTr,
            CriticalCount = findings.Count(f => f.Severity == Severity.Critical),
            HighCount = findings.Count(f => f.Severity == Severity.High),
            MediumCount = findings.Count(f => f.Severity == Severity.Medium),
            LowCount = findings.Count(f => f.Severity == Severity.Low),
            InfoCount = findings.Count(f => f.Severity == Severity.Informational),
            ConfirmedCount = findings.Count(f => f.ConfidenceLevel == ConfidenceLevel.Confirmed),
            StrongIndicationCount = findings.Count(f => f.ConfidenceLevel == ConfidenceLevel.StrongIndication),
            RecommendationCount = findings.Count(f => f.ConfidenceLevel == ConfidenceLevel.Recommendation),
            ChecksTotal = checksTotal,
            ChecksPassed = checksPassed,
            ChecksFailed = checksFailed,
            ChecksSkipped = checksSkipped
        };

        foreach (var f in findings)
        {
            f.ScanResultId = result.Id;
        }
        result.Findings = findings;

        _dbContext.ScanResults.Add(result);
        job.Status = ScanStatus.Completed;
        job.CompletedAt = _clock.UtcNow;
        job.ProgressPercentage = 100;
        job.CurrentStep = "Tamamlandı";

        if (job.IsRetest && job.RetestForFindingId is Guid retestFindingId && previousResult is not null)
        {
            var originalFinding = previousResult.Findings.FirstOrDefault(f => f.Id == retestFindingId);
            if (originalFinding is not null)
            {
                var matching = findings.FirstOrDefault(f =>
                    !string.IsNullOrEmpty(originalFinding.Fingerprint) &&
                    f.Fingerprint == originalFinding.Fingerprint);

                var retestResult = matching is null
                    ? RetestResult.Resolved
                    : matching.Severity < originalFinding.Severity
                        ? RetestResult.Improved
                        : matching.Severity > originalFinding.Severity
                            ? RetestResult.Regressed
                            : RetestResult.StillPresent;

                _dbContext.RetestComparisons.Add(new RetestComparison
                {
                    CompanyId = job.CompanyId,
                    OriginalFindingId = originalFinding.Id,
                    PreviousScanResultId = previousResult.Id,
                    CurrentScanResultId = result.Id,
                    PreviousSeverity = originalFinding.Severity,
                    CurrentSeverity = matching?.Severity,
                    PreviousConfidence = originalFinding.ConfidenceLevel,
                    CurrentConfidence = matching?.ConfidenceLevel,
                    Result = retestResult,
                    ComparisonSummary = retestResult switch
                    {
                        RetestResult.Resolved => "Yeniden testte bulgu tekrar üretilmedi.",
                        RetestResult.Improved => "Bulgu hâlâ tekrar üretilebiliyor ancak şiddeti düştü.",
                        RetestResult.Regressed => "Bulgu daha yüksek şiddette tekrar üretildi.",
                        RetestResult.StillPresent => "Bulgu yeniden testte aynı kalıntıyla üretildi.",
                        _ => null
                    },
                    RequestedByUserId = job.RequestedByUserId
                });

                if (retestResult == RetestResult.Resolved)
                {
                    originalFinding.Status = FindingStatus.Fixed;
                    originalFinding.LastSeenAt = _clock.UtcNow;
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.Notifications.Add(new Notification
        {
            CompanyId = job.CompanyId,
            UserId = job.RequestedByUserId,
            Title = job.IsRetest ? "Yeniden test tamamlandı" : "Tarama tamamlandı",
            Message = $"{job.DomainAsset?.HostName} için {findings.Count} bulgu üretildi. Güvenlik puanı: {scoreResult.Score}/100 ({scoreResult.Grade}).",
            Type = findings.Any(f => f.Severity >= Severity.High) ? NotificationType.Warning : NotificationType.Success,
            RelatedEntityType = "ScanJob",
            RelatedEntityId = job.Id,
            ActionUrl = $"/scans/{job.Id}",
            Icon = "shield-check"
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        var completedPayload = new
        {
            scanJobId = job.Id,
            score = scoreResult.Score,
            findings = findings.Count,
            isRetest = job.IsRetest,
            host = job.DomainAsset?.HostName
        };
        await _activity.PublishToCompanyAsync(job.CompanyId, "scan.completed", completedPayload, cancellationToken);
        await _activity.PublishToUserAsync(job.RequestedByUserId, "scan.completed", completedPayload, cancellationToken);
        await _activity.PublishToSystemAdminsAsync("scan.completed", completedPayload, cancellationToken);
    }

    private async Task PublishProgressAsync(ScanJob job)
    {
        var payload = new
        {
            scanJobId = job.Id,
            progress = job.ProgressPercentage,
            currentStep = job.CurrentStep,
            status = job.Status.ToString(),
            host = job.DomainAsset?.HostName
        };
        await _activity.PublishToCompanyAsync(job.CompanyId, "scan.progress", payload);
        await _activity.PublishToUserAsync(job.RequestedByUserId, "scan.progress", payload);
        await _activity.PublishToSystemAdminsAsync("scan.progress", payload);
    }
}
