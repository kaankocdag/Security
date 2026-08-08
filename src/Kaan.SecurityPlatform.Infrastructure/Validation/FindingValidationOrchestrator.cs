using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Validation;
using Kaan.SecurityPlatform.Application.Features.Validation.Dtos;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Entities.Validation;
using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kaan.SecurityPlatform.Infrastructure.Validation;

public sealed class FindingValidationOrchestrator : IFindingValidationOrchestrator
{
    private static readonly HashSet<Guid> ActiveTargets = new();
    private static readonly object ActiveLock = new();

    private readonly IApplicationDbContext _db;
    private readonly IValidatorRegistry _registry;
    private readonly IValidationPolicyEngine _policy;
    private readonly IScopePolicyValidator _scope;
    private readonly IAuthorizationEvidenceService _auth;
    private readonly IValidationRunService _runs;
    private readonly IImpactAssessmentService _impact;
    private readonly ISubmissionEligibilityEvaluator _eligibility;
    private readonly IValidationAuditService _audit;
    private readonly ValidationHttpGate _httpGate;
    private readonly ICurrentUser _currentUser;
    private readonly IHostEnvironment _env;
    private readonly IOptions<ValidationOptions> _options;
    private readonly ILogger<FindingValidationOrchestrator> _logger;

    public FindingValidationOrchestrator(
        IApplicationDbContext db,
        IValidatorRegistry registry,
        IValidationPolicyEngine policy,
        IScopePolicyValidator scope,
        IAuthorizationEvidenceService auth,
        IValidationRunService runs,
        IImpactAssessmentService impact,
        ISubmissionEligibilityEvaluator eligibility,
        IValidationAuditService audit,
        ValidationHttpGate httpGate,
        ICurrentUser currentUser,
        IHostEnvironment env,
        IOptions<ValidationOptions> options,
        ILogger<FindingValidationOrchestrator> logger)
    {
        _db = db;
        _registry = registry;
        _policy = policy;
        _scope = scope;
        _auth = auth;
        _runs = runs;
        _impact = impact;
        _eligibility = eligibility;
        _audit = audit;
        _httpGate = httpGate;
        _currentUser = currentUser;
        _env = env;
        _options = options;
        _logger = logger;
    }

    public async Task<Result<ValidationPreconditionsDto>> GetPreconditionsAsync(
        Guid findingId,
        CancellationToken cancellationToken = default)
    {
        var finding = await LoadFindingAsync(findingId, cancellationToken);
        if (finding is null)
        {
            return Result<ValidationPreconditionsDto>.Failure("finding_not_found", "Bulgu bulunamadı.");
        }

        var targetId = finding.ScanResult?.ScanJob?.DomainAssetId;
        if (targetId is null)
        {
            return Result<ValidationPreconditionsDto>.Failure("target_missing", "Bulgu hedefi (domain) yok.");
        }

        var validator = _registry.Resolve(finding);
        var scope = await _scope.GetActiveAsync(targetId.Value, cancellationToken);
        var auth = await _auth.GetActiveAsync(targetId.Value, cancellationToken);
        var ctx = await BuildContextAsync(finding, targetId.Value, null!, scope, auth, false, null, cancellationToken);
        // Placeholder run for precondition checks only
        ctx = ctx with { Run = new FindingValidationRun { Id = Guid.Empty, FindingId = finding.Id, TargetId = targetId.Value } };

        var pre = await validator.CheckPreconditionsAsync(ctx, cancellationToken);
        var policy = await _policy.EvaluateAsync(ctx, cancellationToken);
        var missing = pre.MissingItems.ToList();
        if (!policy.Allowed && policy.BlockReason is not null && !missing.Contains(policy.BlockReason))
        {
            missing.Add(policy.BlockReason);
        }

        var manualOnly = validator.AutomationKind == ValidationAutomationKind.ManualOnly;
        return Result<ValidationPreconditionsDto>.Success(new ValidationPreconditionsDto(
            finding.Id,
            validator.ValidatorType,
            validator.AutomationKind,
            validator.RiskLevel,
            CanStartAutomatic: pre.CanStart && policy.Allowed && !manualOnly,
            ManualOnly: manualOnly,
            MissingItems: missing,
            TargetInBountyScope: policy.TargetInBountyScope,
            TestingMethodAllowed: policy.TestingMethodAllowed,
            AuthorizationValid: policy.AuthorizationValid,
            HasScopePolicy: scope is not null,
            HasAuthorizationEvidence: auth is not null,
            Disclaimer:
                "TargetInBountyScope ≠ reward. Candidate ≠ Confirmed. Reward is never guaranteed. " +
                "Default mode is passive/read-only."));
    }

    public async Task<Result<FindingValidationRunDto>> StartAsync(
        StartFindingValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        var finding = await LoadFindingAsync(request.FindingId, cancellationToken);
        if (finding is null)
        {
            return Result<FindingValidationRunDto>.Failure("finding_not_found", "Bulgu bulunamadı.");
        }

        var targetId = finding.ScanResult?.ScanJob?.DomainAssetId;
        if (targetId is null)
        {
            return Result<FindingValidationRunDto>.Failure("target_missing", "Bulgu hedefi yok.");
        }

        if (!request.ExplicitUserApproval)
        {
            return Result<FindingValidationRunDto>.Failure(
                "approval_required",
                "Aktif doğrulama için açık kullanıcı onayı zorunludur.");
        }

        lock (ActiveLock)
        {
            if (ActiveTargets.Contains(targetId.Value))
            {
                return Result<FindingValidationRunDto>.Failure(
                    "target_busy",
                    "Bu hedefte zaten çalışan bir doğrulama var.");
            }

            ActiveTargets.Add(targetId.Value);
        }

        try
        {
            var validator = _registry.Resolve(finding);
            if (validator.AutomationKind == ValidationAutomationKind.ManualOnly)
            {
                var manualRun = await _runs.CreateAwaitingApprovalAsync(
                    finding, targetId.Value, validator, _currentUser.UserId, cancellationToken);
                manualRun.UserApprovedAt = DateTime.UtcNow;
                await _runs.MarkRunningAsync(manualRun, cancellationToken);
                var scopeM = await _scope.GetActiveAsync(targetId.Value, cancellationToken);
                var authM = await _auth.GetActiveAsync(targetId.Value, cancellationToken);
                var ctxM = await BuildContextAsync(
                    finding, targetId.Value, manualRun, scopeM, authM, true, request.OwnedTestResourceUrl, cancellationToken);
                var execM = await validator.ValidateAsync(ctxM, _httpGate, cancellationToken);
                return Result<FindingValidationRunDto>.Success(
                    await FinalizeAsync(finding, manualRun, execM, ctxM, cancellationToken));
            }

            var scope = await _scope.GetActiveAsync(targetId.Value, cancellationToken);
            var auth = await _auth.GetActiveAsync(targetId.Value, cancellationToken);
            var run = await _runs.CreateAwaitingApprovalAsync(
                finding, targetId.Value, validator, _currentUser.UserId, cancellationToken);
            run.UserApprovedAt = DateTime.UtcNow;
            run.AuthorizationEvidenceId = auth?.Id;
            run.ScopePolicyId = scope?.Id;
            await _db.SaveChangesAsync(cancellationToken);

            var ctx = await BuildContextAsync(
                finding, targetId.Value, run, scope, auth, true, request.OwnedTestResourceUrl, cancellationToken,
                request.TestAccountAId, request.TestAccountBId);

            var policy = await _policy.EvaluateAsync(ctx, cancellationToken);
            if (!policy.Allowed)
            {
                run.Status = policy.BlockStatus ?? ValidationStatus.BlockedByPolicy;
                run.StopReason = policy.BlockReason;
                run.ErrorCode = "blocked_by_policy";
                run.ErrorMessage = policy.BlockReason;
                run.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                await _audit.WriteAsync("blocked", run.Id, new { policy.BlockReason }, cancellationToken);
                return Result<FindingValidationRunDto>.Success((await MapRunAsync(run.Id, cancellationToken))!);
            }

            var pre = await validator.CheckPreconditionsAsync(ctx, cancellationToken);
            if (!pre.CanStart)
            {
                run.Status = ValidationStatus.PreconditionsMissing;
                run.ErrorMessage = string.Join("; ", pre.MissingItems);
                run.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                return Result<FindingValidationRunDto>.Success((await MapRunAsync(run.Id, cancellationToken))!);
            }

            await _runs.MarkRunningAsync(run, cancellationToken);
            await _audit.WriteAsync("started", run.Id, new { validator.ValidatorType }, cancellationToken);

            ValidatorExecutionResult exec;
            try
            {
                exec = await validator.ValidateAsync(ctx, _httpGate, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Validation failed for finding {FindingId}", finding.Id);
                run.Status = ValidationStatus.Failed;
                run.ErrorCode = "validation_exception";
                run.ErrorMessage = "Validation stopped safely.";
                run.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                return Result<FindingValidationRunDto>.Success((await MapRunAsync(run.Id, cancellationToken))!);
            }

            return Result<FindingValidationRunDto>.Success(
                await FinalizeAsync(finding, run, exec, ctx, cancellationToken));
        }
        finally
        {
            lock (ActiveLock)
            {
                ActiveTargets.Remove(targetId.Value);
            }
        }
    }

    public async Task<Result<FindingValidationRunDto>> GetRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var dto = await MapRunAsync(runId, cancellationToken);
        return dto is null
            ? Result<FindingValidationRunDto>.Failure("run_not_found", "Doğrulama çalışması bulunamadı.")
            : Result<FindingValidationRunDto>.Success(dto);
    }

    public async Task<Result<FindingValidationRunDto>> StopAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var run = await _db.FindingValidationRuns.FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        if (run is null)
        {
            return Result<FindingValidationRunDto>.Failure("run_not_found", "Doğrulama çalışması bulunamadı.");
        }

        run.StopRequested = true;
        _httpGate.RequestStop();
        if (run.Status == ValidationStatus.Running)
        {
            run.Status = ValidationStatus.Stopped;
            run.StopReason = "User emergency stop";
            run.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("stopped", run.Id, null, cancellationToken);
        return Result<FindingValidationRunDto>.Success((await MapRunAsync(runId, cancellationToken))!);
    }

    public async Task<IReadOnlyList<FindingValidationRunDto>> ListRunsForFindingAsync(
        Guid findingId,
        CancellationToken cancellationToken = default)
    {
        var ids = await _db.FindingValidationRuns.AsNoTracking()
            .Where(r => r.FindingId == findingId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.Id)
            .Take(20)
            .ToListAsync(cancellationToken);

        var list = new List<FindingValidationRunDto>();
        foreach (var id in ids)
        {
            var dto = await MapRunAsync(id, cancellationToken);
            if (dto is not null)
            {
                list.Add(dto);
            }
        }

        return list;
    }

    private async Task<FindingValidationRunDto> FinalizeAsync(
        Finding finding,
        FindingValidationRun run,
        ValidatorExecutionResult exec,
        ValidationContext ctx,
        CancellationToken cancellationToken)
    {
        foreach (var e in exec.Evidence)
        {
            e.ValidationRunId = run.Id;
            _db.ValidationEvidence.Add(e);
        }

        run.ActualRequestCount = Math.Max(run.ActualRequestCount, exec.Evidence.Count);
        if (exec.ErrorCode == "rate_limited" || exec.Status == ValidationStatus.Stopped)
        {
            run.Status = ValidationStatus.Stopped;
            run.StopReason = exec.ErrorMessage;
        }
        else
        {
            run.Status = exec.Status;
        }

        var policy = await _policy.EvaluateAsync(ctx, cancellationToken);
        var impact = _impact.Assess(exec);
        var eligibility = _eligibility.Evaluate(impact, policy, exec);

        // Never allow SubmitCandidate without confirmed+demonstrated.
        if (!impact.ConfirmedVulnerability || !impact.DemonstratedImpact)
        {
            eligibility = eligibility with
            {
                SubmissionEligible = false,
                PotentialRewardEligible = false
            };
            if (eligibility.Recommendation == ValidationSubmissionRecommendation.SubmitCandidate)
            {
                eligibility = eligibility with
                {
                    Recommendation = ValidationSubmissionRecommendation.ManualReview
                };
            }
        }

        var result = new FindingValidationResult
        {
            ConfirmedVulnerability = impact.ConfirmedVulnerability,
            DemonstratedImpact = impact.DemonstratedImpact,
            ImpactType = impact.ImpactType,
            Confidence = impact.Confidence,
            SubmissionRecommendation = eligibility.Recommendation,
            SubmissionEligible = eligibility.SubmissionEligible,
            PotentialRewardEligible = eligibility.PotentialRewardEligible
                                      && eligibility.Recommendation == ValidationSubmissionRecommendation.SubmitCandidate,
            EligibilityReason = eligibility.EligibilityReason,
            ManualReviewReasons = string.Join("\n", exec.ManualReviewReasons.Select(r => "- " + r)),
            ExpectedResult = exec.ExpectedResult,
            ActualResult = exec.ActualResult,
            ValidatorVersion = "1.0.0",
            ReproductionCount = exec.ReproductionCount,
            TestAccountRolesUsed = exec.TestAccountRolesUsed
        };

        await _runs.CompleteAsync(run, result, cancellationToken);

        // Sync finding cache fields — never auto-submit to HackerOne.
        finding.ConfirmedVulnerability = result.ConfirmedVulnerability;
        finding.DemonstratedImpact = result.DemonstratedImpact;
        finding.LatestValidationStatus = run.Status;
        finding.SubmissionEligible = result.SubmissionEligible;
        finding.PotentialRewardEligible = result.PotentialRewardEligible;
        finding.LatestValidationRunId = run.Id;
        finding.EligibilityReason = result.EligibilityReason;

        if (result.ConfirmedVulnerability && result.DemonstratedImpact)
        {
            finding.FindingClass = FindingClass.Vulnerability;
            finding.SubmissionRecommendation = SubmissionRecommendation.Submit;
            finding.BugBountyEligible = result.SubmissionEligible;
            finding.Exploitability = Exploitability.Demonstrated;
            finding.Fingerprint = "asc.access.confirmed-unauthorized";
        }
        else if (run.Status is ValidationStatus.ManualReviewRequired or ValidationStatus.CandidateOnly)
        {
            if (finding.FindingClass == FindingClass.Vulnerability && !finding.ConfirmedVulnerability)
            {
                finding.FindingClass = FindingClass.VulnerabilityCandidate;
            }

            finding.SubmissionRecommendation = eligibility.Recommendation switch
            {
                ValidationSubmissionRecommendation.ManualReview => SubmissionRecommendation.ManualReview,
                ValidationSubmissionRecommendation.NeedsAdditionalEvidence => SubmissionRecommendation.ManualReview,
                _ => SubmissionRecommendation.DoNotSubmit
            };
            finding.BugBountyEligible = false;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.WriteAsync("completed", run.Id, new
        {
            run.Status,
            result.ConfirmedVulnerability,
            result.DemonstratedImpact,
            result.SubmissionEligible,
            result.PotentialRewardEligible
        }, cancellationToken);

        return (await MapRunAsync(run.Id, cancellationToken))!;
    }

    private async Task<ValidationContext> BuildContextAsync(
        Finding finding,
        Guid targetId,
        FindingValidationRun run,
        ScopePolicy? scope,
        ValidationAuthorizationEvidence? auth,
        bool approved,
        string? ownedResource,
        CancellationToken cancellationToken,
        Guid? testAccountAId = null,
        Guid? testAccountBId = null)
    {
        var host = finding.ScanResult?.ScanJob?.DomainAsset?.HostName ?? "invalid.local";
        Uri? affected = null;
        if (!string.IsNullOrWhiteSpace(finding.AffectedUrl)
            && Uri.TryCreate(finding.AffectedUrl, UriKind.Absolute, out var parsed))
        {
            affected = parsed;
        }

        var accountsQuery = _db.TestAccountSessions.Where(a => a.TargetId == targetId);
        if (testAccountAId is not null || testAccountBId is not null)
        {
            accountsQuery = accountsQuery.Where(a =>
                a.Id == testAccountAId || a.Id == testAccountBId);
        }

        var accounts = await accountsQuery.ToListAsync(cancellationToken);
        var mock = _options.Value.AllowMockAuthorization && _env.IsDevelopment();

        return new ValidationContext(
            finding,
            targetId,
            host,
            affected,
            scope,
            auth,
            accounts,
            run,
            approved,
            mock || _env.IsDevelopment(),
            ownedResource);
    }

    private async Task<Finding?> LoadFindingAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Findings
            .Include(f => f.ScanResult!).ThenInclude(r => r.ScanJob!).ThenInclude(j => j.DomainAsset)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    private async Task<FindingValidationRunDto?> MapRunAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await _db.FindingValidationRuns.AsNoTracking()
            .Include(r => r.Result)
            .Include(r => r.EvidenceItems)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        if (run is null)
        {
            return null;
        }

        FindingValidationResultDto? resultDto = null;
        if (run.Result is { } res)
        {
            var reasons = string.IsNullOrWhiteSpace(res.ManualReviewReasons)
                ? Array.Empty<string>()
                : res.ManualReviewReasons.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(x => x.TrimStart('-', ' '))
                    .ToArray();

            resultDto = new FindingValidationResultDto(
                res.ConfirmedVulnerability,
                res.DemonstratedImpact,
                res.ImpactType,
                res.Confidence,
                res.SubmissionRecommendation,
                res.SubmissionEligible,
                res.PotentialRewardEligible,
                res.EligibilityReason,
                reasons,
                res.ExpectedResult,
                res.ActualResult,
                res.ValidatorVersion,
                res.ReproductionCount,
                res.TestAccountRolesUsed,
                "Reward not guaranteed.");
        }

        var evidence = run.EvidenceItems.OrderBy(e => e.CapturedAt).Select(e => new ValidationEvidenceDto(
            e.Id, e.EvidenceType, e.RequestMethod, e.RedactedRequestUrl, e.ResponseStatusCode,
            e.FinalUrl, e.RedirectChain, e.ResponseContentType, e.ResponseHash, e.RedactedResponseExcerpt,
            e.SessionRole, e.CapturedAt)).ToList();

        return new FindingValidationRunDto(
            run.Id, run.FindingId, run.TargetId, run.ValidatorType, run.ValidationMode, run.Status,
            run.RiskLevel, run.StartedAt, run.CompletedAt, run.MaxRequestCount, run.ActualRequestCount,
            run.StopReason, run.ErrorCode, run.ErrorMessage, resultDto, evidence);
    }
}
