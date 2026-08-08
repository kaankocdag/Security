using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Features.BugBounty;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Entities.Notifications;
using Kaan.SecurityPlatform.Domain.Entities.Scans;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.BugBounty;
using Kaan.SecurityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Executor;

public sealed class ApplicationSecurityCandidateExecutor
{
    private readonly SecurityPlatformDbContext _dbContext;
    private readonly IEnumerable<IApplicationSecurityCandidateEngine> _engines;
    private readonly ISecurityScoreCalculator _scoreCalculator;
    private readonly IFindingValidationClassifier _classifier;
    private readonly IRootCauseGroupService _rootCauseGroups;
    private readonly IActivityEventPublisher _activity;
    private readonly IDateTimeProvider _clock;
    private readonly HackerOneOptions _options;
    private readonly ILogger<ApplicationSecurityCandidateExecutor> _logger;

    public ApplicationSecurityCandidateExecutor(
        SecurityPlatformDbContext dbContext,
        IEnumerable<IApplicationSecurityCandidateEngine> engines,
        ISecurityScoreCalculator scoreCalculator,
        IFindingValidationClassifier classifier,
        IRootCauseGroupService rootCauseGroups,
        IActivityEventPublisher activity,
        IDateTimeProvider clock,
        IOptions<HackerOneOptions> options,
        ILogger<ApplicationSecurityCandidateExecutor> logger)
    {
        _dbContext = dbContext;
        _engines = engines;
        _scoreCalculator = scoreCalculator;
        _classifier = classifier;
        _rootCauseGroups = rootCauseGroups;
        _activity = activity;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid scanJobId, CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.ScanJobs
            .IgnoreQueryFilters()
            .Include(j => j.DomainAsset)
            .FirstOrDefaultAsync(j => j.Id == scanJobId, cancellationToken);

        if (job?.DomainAsset is null)
        {
            return;
        }

        if (job.AssessmentMode != AssessmentMode.ApplicationSecurityCandidate)
        {
            job.Status = ScanStatus.Failed;
            job.ErrorMessage = "ApplicationSecurityCandidateExecutor yalnızca ApplicationSecurityCandidate modunu çalıştırır.";
            job.CompletedAt = _clock.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        if (!job.DomainAsset.IsVerified)
        {
            job.Status = ScanStatus.Failed;
            job.ErrorMessage = "ApplicationSecurityCandidate için domain doğrulanmış olmalıdır.";
            job.CompletedAt = _clock.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var engineList = _engines.ToList();
        job.TotalSteps = engineList.Count;
        job.CompletedSteps = 0;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var scheme = string.IsNullOrEmpty(job.DomainAsset.Scheme) ? "https" : job.DomainAsset.Scheme;
        var uri = new Uri($"{scheme}://{job.DomainAsset.NormalizedHostName}/");
        var userAgent = _options.AmazonVrp.UserAgent;
        var context = new CandidateEngineContext(
            job.CompanyId,
            Guid.Empty,
            job.DomainAsset.NormalizedHostName,
            uri,
            userAgent,
            null,
            null);

        // Runtime throttle: yapılandırılmış dakikalık limitten motorlar arası minimum gecikme türet.
        var ratePerMinute = Math.Max(1, _options.AmazonVrp.RateLimitPerMinute);
        var interStepDelay = TimeSpan.FromMilliseconds(Math.Min(5000, 60_000 / ratePerMinute));

        var findings = new List<Finding>();
        var enginesCompleted = new List<string>();
        var enginesFailed = new List<string>();
        var firstEngine = true;
        foreach (var engine in engineList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!firstEngine)
            {
                await Task.Delay(interStepDelay, cancellationToken);
            }

            firstEngine = false;
            job.CurrentStep = engine.EngineKey;
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                using var stepCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                stepCts.CancelAfter(TimeSpan.FromSeconds(25));
                var drafts = await engine.RunAsync(context, stepCts.Token);
                foreach (var d in drafts)
                {
                    var entity = new Finding
                    {
                        CompanyId = job.CompanyId,
                        Title = d.Title,
                        Description = d.Description,
                        Severity = d.Severity,
                        ConfidenceLevel = ConfidenceLevel.StrongIndication,
                        Category = d.Category,
                        CweCode = d.CweCode,
                        OwaspCategory = d.OwaspCategory,
                        AffectedUrl = d.AffectedUrl,
                        AffectedParameter = d.AffectedParameter,
                        Evidence = d.Evidence,
                        Remediation = d.Remediation,
                        CheckCode = d.CheckCode,
                        Fingerprint = d.Fingerprint,
                        FirstSeenAt = _clock.UtcNow,
                        LastSeenAt = _clock.UtcNow
                    };
                    if (d.Reflection is { } r)
                    {
                        entity.ReflectionContext = r.Context;
                        entity.ReflectionCount = r.ReflectionCount;
                        entity.HtmlEncoded = r.HtmlEncoded;
                        entity.AttributeEncoded = r.AttributeEncoded;
                        entity.ReflectionContentType = r.ContentType;
                        entity.ReflectionHttpStatus = r.HttpStatus;
                        entity.ReflectionLocation = r.ReflectionLocation;
                        entity.InputSource = r.InputSource;
                        entity.ReflectionMarker = r.Marker.Length > 64 ? r.Marker[..64] : r.Marker;
                    }

                    _classifier.Classify(entity, AmazonVrpPolicy.PolicyKeyConstant);
                    findings.Add(entity);
                }

                enginesCompleted.Add(engine.EngineKey);
            }
            catch (Exception ex)
            {
                enginesFailed.Add(engine.EngineKey);
                _logger.LogWarning(ex, "Candidate engine failed: {Engine}", engine.EngineKey);
            }

            job.CompletedSteps++;
            job.ProgressPercentage = Math.Min(100, job.CompletedSteps * 100 / Math.Max(job.TotalSteps, 1));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var scoreResult = _scoreCalculator.Calculate(findings);
        var manualReviewCount = findings.Count(f => f.SubmissionRecommendation == SubmissionRecommendation.ManualReview);
        var submitCount = findings.Count(f => f.SubmissionRecommendation == SubmissionRecommendation.Submit);
        var infoOnlyCount = findings.Count(f => f.SubmissionRecommendation == SubmissionRecommendation.DoNotSubmit);
        var enginesLabel = enginesCompleted.Count > 0
            ? string.Join(", ", enginesCompleted)
            : "(none)";
        var failedLabel = enginesFailed.Count > 0
            ? $" Başarısız motorlar: {string.Join(", ", enginesFailed)}."
            : string.Empty;

        var result = new ScanResult
        {
            CompanyId = job.CompanyId,
            ScanJobId = job.Id,
            SecurityScore = scoreResult.Score,
            StartedAt = job.StartedAt ?? _clock.UtcNow,
            CompletedAt = _clock.UtcNow,
            Summary =
                $"ASC tamamlandı. Çalışan motorlar: {enginesLabel}.{failedLabel} " +
                $"Gözlem={findings.Count} (Submit={submitCount}, ManualReview={manualReviewCount}, Informational/DoNotSubmit={infoOnlyCount}). " +
                $"Puan: {scoreResult.Score}/100.",
            ExecutiveSummary =
                findings.Count == 0
                    ? "Güvenli candidate motorları tamamlandı; aday bulgu üretilmedi. Bu bir güvenlik tarama özetidir (otomatik HackerOne gönderimi yok)."
                    : "Güvenli candidate motorları tamamlandı. Otomatik HackerOne gönderimi yok — ManualReview/Submit için rapor builder kullanın.",
            CriticalCount = findings.Count(f => f.Severity == Severity.Critical),
            HighCount = findings.Count(f => f.Severity == Severity.High),
            MediumCount = findings.Count(f => f.Severity == Severity.Medium),
            LowCount = findings.Count(f => f.Severity == Severity.Low),
            InfoCount = findings.Count(f => f.Severity == Severity.Informational),
            ChecksTotal = engineList.Count,
            ChecksPassed = enginesCompleted.Count,
            ChecksFailed = enginesFailed.Count,
            ChecksSkipped = 0
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
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var f in findings)
        {
            await _rootCauseGroups.AssignAsync(f.Id, f.Fingerprint, f.Title, cancellationToken);
        }

        await _activity.PublishToCompanyAsync(job.CompanyId, "scan.completed", new
        {
            scanJobId = job.Id,
            findingCount = findings.Count,
            assessmentMode = job.AssessmentMode.ToString()
        }, cancellationToken);

        _dbContext.Notifications.Add(new Notification
        {
            CompanyId = job.CompanyId,
            UserId = job.RequestedByUserId,
            Title = "Application Security Candidate tamamlandı",
            Message = $"{job.DomainAsset.HostName}: {findings.Count} aday bulgu (otomatik H1 gönderimi yok).",
            Type = NotificationType.Info,
            RelatedEntityType = "ScanJob",
            RelatedEntityId = job.Id,
            ActionUrl = $"/scans/{job.Id}",
            Icon = "bug"
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
