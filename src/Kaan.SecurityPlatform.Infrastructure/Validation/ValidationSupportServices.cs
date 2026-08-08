using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Validation;
using Kaan.SecurityPlatform.Application.Features.Validation.Dtos;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Entities.Validation;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kaan.SecurityPlatform.Infrastructure.Validation;

public sealed class EvidenceRedactor : IEvidenceRedactor
{
    private static readonly Regex SecretLike = new(
        @"(?i)(authorization|cookie|token|api[_-]?key|password|secret|bearer)\s*[:=]\s*\S+|Bearer\s+[A-Za-z0-9\-._~+/]+=*|([a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})",
        RegexOptions.Compiled);

    public string RedactUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        try
        {
            var uri = new Uri(url, UriKind.Absolute);
            var builder = new UriBuilder(uri) { Query = string.IsNullOrEmpty(uri.Query) ? string.Empty : "[redacted]" };
            return builder.Uri.ToString();
        }
        catch
        {
            return "[redacted-url]";
        }
    }

    public string RedactBody(string? body)
    {
        if (string.IsNullOrEmpty(body))
        {
            return string.Empty;
        }

        var trimmed = body.Length > 800 ? body[..800] : body;
        return SecretLike.Replace(trimmed, "[redacted]");
    }

    public string HashBody(string? body)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(body ?? string.Empty));
        return Convert.ToHexString(bytes.AsSpan(0, 8)).ToLowerInvariant();
    }
}

public sealed class EvidenceCollector(IEvidenceRedactor redactor) : IEvidenceCollector
{
    public ValidationEvidence CreateHttpEvidence(
        Guid runId,
        ValidationSessionRole role,
        string method,
        string url,
        int status,
        string? finalUrl,
        IReadOnlyList<string> redirectChain,
        string? contentType,
        string? bodyExcerpt,
        string? responseHash) =>
        new()
        {
            ValidationRunId = runId,
            EvidenceType = ValidationEvidenceType.HttpObservation,
            RequestMethod = method,
            RedactedRequestUrl = redactor.RedactUrl(url),
            ResponseStatusCode = status,
            FinalUrl = redactor.RedactUrl(finalUrl),
            RedirectChain = redirectChain.Count == 0 ? null : string.Join(" → ", redirectChain.Select(redactor.RedactUrl)),
            ResponseContentType = contentType,
            ResponseHash = responseHash ?? redactor.HashBody(bodyExcerpt),
            RedactedResponseExcerpt = redactor.RedactBody(bodyExcerpt),
            SessionRole = role,
            CapturedAt = DateTime.UtcNow
        };
}

public sealed class ScopePolicyValidator(IApplicationDbContext db) : IScopePolicyValidator
{
    public async Task<ScopePolicy?> GetActiveAsync(Guid targetId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await db.ScopePolicies
            .Where(p => p.TargetId == targetId)
            .OrderByDescending(p => p.LastVerifiedAt ?? p.CreatedAt)
            .FirstOrDefaultAsync(p =>
                p.ScopeStatus == ScopePolicyStatus.InScope
                && (p.ValidFrom == null || p.ValidFrom <= now)
                && (p.ValidUntil == null || p.ValidUntil >= now), cancellationToken);
    }

    public bool IsMethodAllowed(ScopePolicy policy, string method)
    {
        var m = method.Trim().ToUpperInvariant();
        if (Split(policy.ProhibitedTestMethods).Contains(m))
        {
            return false;
        }

        var allowed = Split(policy.AllowedTestMethods);
        return allowed.Count == 0 || allowed.Contains(m);
    }

    public bool IsTestTypeAllowed(ScopePolicy policy, string testType)
    {
        var t = testType.Trim().ToUpperInvariant();
        return !Split(policy.ProhibitedTestMethods).Contains(t);
    }

    private static HashSet<string> Split(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}

public sealed class AuthorizationEvidenceService(
    IApplicationDbContext db,
    ICurrentUser currentUser) : IAuthorizationEvidenceService
{
    public async Task<ValidationAuthorizationEvidence?> GetActiveAsync(
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await db.ValidationAuthorizationEvidence
            .Where(e => e.TargetId == targetId && e.IsActive && e.ValidFrom <= now && e.ValidUntil >= now)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<ValidationAuthorizationEvidenceDto>> UpsertAsync(
        UpsertAuthorizationEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (currentUser.CompanyId is null)
        {
            return Result<ValidationAuthorizationEvidenceDto>.Failure("no_tenant", "Şirket bağlamı yok.");
        }

        var entity = await db.ValidationAuthorizationEvidence
            .FirstOrDefaultAsync(e => e.TargetId == request.TargetId && e.IsActive, cancellationToken);
        if (entity is null)
        {
            entity = new ValidationAuthorizationEvidence
            {
                CompanyId = currentUser.CompanyId.Value,
                TargetId = request.TargetId
            };
            db.ValidationAuthorizationEvidence.Add(entity);
        }

        entity.AuthorizedByName = request.AuthorizedByName.Trim();
        entity.AuthorizedByEmail = request.AuthorizedByEmail.Trim();
        entity.ScopeSummary = request.ScopeSummary.Trim();
        entity.AllowedTestTypes = request.AllowedTestTypes.Trim();
        entity.ValidFrom = request.ValidFrom.ToUniversalTime();
        entity.ValidUntil = request.ValidUntil.ToUniversalTime();
        entity.EvidenceNotes = request.EvidenceNotes;
        entity.AuthorizationRecordId = request.AuthorizationRecordId;
        entity.IsActive = true;
        await db.SaveChangesAsync(cancellationToken);

        return Result<ValidationAuthorizationEvidenceDto>.Success(Map(entity));
    }

    private static ValidationAuthorizationEvidenceDto Map(ValidationAuthorizationEvidence e) =>
        new(e.Id, e.TargetId, e.AuthorizedByName, e.AuthorizedByEmail, e.ScopeSummary,
            e.AllowedTestTypes, e.ValidFrom, e.ValidUntil, e.IsActive);
}

public sealed class ValidationPolicyEngine(
    IScopePolicyValidator scopeValidator,
    IAuthorizationEvidenceService authEvidence,
    IHostEnvironment env,
    IOptions<ValidationOptions> options) : IValidationPolicyEngine
{
    public async Task<PolicyDecision> EvaluateAsync(ValidationContext context, CancellationToken cancellationToken = default)
    {
        var mockOk = options.Value.AllowMockAuthorization && env.IsDevelopment();
        var scope = context.ScopePolicy ?? await scopeValidator.GetActiveAsync(context.TargetId, cancellationToken);
        var auth = context.AuthorizationEvidence ?? await authEvidence.GetActiveAsync(context.TargetId, cancellationToken);

        if (!mockOk && scope is null)
        {
            return new PolicyDecision(false, false, false, false,
                "ScopePolicy missing or not InScope.", ValidationStatus.BlockedByPolicy);
        }

        if (!mockOk && auth is null)
        {
            return new PolicyDecision(false, scope?.TargetInBountyScope == true, false, false,
                "AuthorizationEvidence missing or expired.", ValidationStatus.BlockedByPolicy);
        }

        var inScope = mockOk || (scope is not null && scope.ScopeStatus == ScopePolicyStatus.InScope);
        var methodOk = mockOk || (scope is not null && scopeValidator.IsMethodAllowed(scope, "GET")
                                                     && scopeValidator.IsTestTypeAllowed(scope, "SAFE-DIFFERENTIAL"));
        var authOk = mockOk || auth is not null;

        if (!inScope)
        {
            return new PolicyDecision(false, false, methodOk, authOk,
                "Target is out of scope.", ValidationStatus.BlockedByPolicy);
        }

        if (!methodOk)
        {
            return new PolicyDecision(false, inScope, false, authOk,
                "Testing method not allowed by ScopePolicy.", ValidationStatus.BlockedByPolicy);
        }

        if (!authOk)
        {
            return new PolicyDecision(false, inScope, methodOk, false,
                "AuthorizationEvidence required.", ValidationStatus.BlockedByPolicy);
        }

        return new PolicyDecision(true, scope?.TargetInBountyScope == true || mockOk, methodOk, authOk, null, null);
    }
}

public sealed class ImpactAssessmentService : IImpactAssessmentService
{
    public ImpactAssessment Assess(ValidatorExecutionResult execution) =>
        new(execution.ConfirmedVulnerability, execution.DemonstratedImpact, execution.ImpactType, execution.Confidence);
}

public sealed class SubmissionEligibilityEvaluator : ISubmissionEligibilityEvaluator
{
    public EligibilityDecision Evaluate(
        ImpactAssessment impact,
        PolicyDecision policy,
        ValidatorExecutionResult execution)
    {
        // TargetInBountyScope alone never grants submission.
        if (!policy.Allowed || !policy.AuthorizationValid || !policy.TestingMethodAllowed)
        {
            return new EligibilityDecision(
                ValidationSubmissionRecommendation.DoNotSubmit,
                false,
                false,
                policy.BlockReason ?? "Blocked by validation policy.");
        }

        if (!impact.ConfirmedVulnerability || !impact.DemonstratedImpact)
        {
            var rec = execution.Status is ValidationStatus.ManualReviewRequired or ValidationStatus.Inconclusive
                ? ValidationSubmissionRecommendation.ManualReview
                : ValidationSubmissionRecommendation.DoNotSubmit;

            if (execution.ManualReviewReasons.Count > 0 && rec == ValidationSubmissionRecommendation.DoNotSubmit)
            {
                rec = ValidationSubmissionRecommendation.ManualReview;
            }

            return new EligibilityDecision(
                rec,
                false,
                false,
                execution.ActualResult.Contains("Administrative path reachability", StringComparison.OrdinalIgnoreCase)
                    ? "Administrative path reachability alone does not demonstrate an access-control vulnerability."
                    : "Candidate only — ConfirmedVulnerability/DemonstratedImpact not met. Reward not guaranteed.");
        }

        if (execution.ReproductionCount < 1 || execution.Evidence.Count == 0)
        {
            return new EligibilityDecision(
                ValidationSubmissionRecommendation.NeedsAdditionalEvidence,
                false,
                false,
                "Impact claimed but reproducible redacted evidence is incomplete.");
        }

        // SubmitCandidate only with full proof; still not auto-submit to HackerOne.
        return new EligibilityDecision(
            ValidationSubmissionRecommendation.SubmitCandidate,
            true,
            true,
            "SubmissionCandidate: verified impact with scope+authorization. Reward not guaranteed.");
    }
}

public sealed class ValidationAuditService(
    IApplicationDbContext db,
    ICurrentUser currentUser) : IValidationAuditService
{
    public async Task WriteAsync(string action, Guid? runId, object? details, CancellationToken cancellationToken = default)
    {
        db.BugBountyAuditLogs.Add(new Domain.Entities.BugBounty.BugBountyAuditLog
        {
            Action = $"validation.{action}",
            EntityType = "FindingValidationRun",
            EntityId = runId?.ToString(),
            DetailsJson = details is null ? null : System.Text.Json.JsonSerializer.Serialize(details),
            ActorUserId = currentUser.UserId,
            ActorEmail = currentUser.Email
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class ValidationRunService(IApplicationDbContext db, IOptions<ValidationOptions> options) : IValidationRunService
{
    public async Task<FindingValidationRun> CreateAwaitingApprovalAsync(
        Finding finding,
        Guid targetId,
        IFindingValidator validator,
        Guid? requestedBy,
        CancellationToken cancellationToken = default)
    {
        var run = new FindingValidationRun
        {
            CompanyId = finding.CompanyId,
            FindingId = finding.Id,
            TargetId = targetId,
            ValidatorType = validator.ValidatorType,
            ValidationMode = validator.DefaultMode,
            Status = ValidationStatus.AwaitingUserApproval,
            RiskLevel = validator.RiskLevel,
            RequestedBy = requestedBy,
            MaxRequestCount = options.Value.DefaultMaxRequestCount
        };
        db.FindingValidationRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);
        return run;
    }

    public async Task MarkRunningAsync(FindingValidationRun run, CancellationToken cancellationToken = default)
    {
        run.Status = ValidationStatus.Running;
        run.StartedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(
        FindingValidationRun run,
        FindingValidationResult result,
        CancellationToken cancellationToken = default)
    {
        result.ValidationRunId = run.Id;
        db.FindingValidationResults.Add(result);
        run.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class TestAccountSecretProtector : ITestAccountSecretProtector
{
    private readonly IDataProtector _protector;

    public TestAccountSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Kaan.SecurityPlatform.Validation.TestAccountSecret.v1");
    }

    public string Protect(string plaintext) => _protector.Protect(plaintext);
    public string Unprotect(string protectedPayload) => _protector.Unprotect(protectedPayload);
}

public sealed class ValidationHttpGate : IValidationHttpGate
{
    private readonly SecureHttpClientFactory _httpFactory;
    private readonly IOptions<ValidationOptions> _options;
    private readonly ILogger<ValidationHttpGate> _logger;
    private int _serverErrors;

    public ValidationHttpGate(
        SecureHttpClientFactory httpFactory,
        IOptions<ValidationOptions> options,
        ILogger<ValidationHttpGate> logger)
    {
        _httpFactory = httpFactory;
        _options = options;
        _logger = logger;
    }

    public bool StopRequested { get; private set; }

    public void RequestStop() => StopRequested = true;

    public async Task<ValidationHttpResponse> SendSafeAsync(
        FindingValidationRun run,
        HttpMethod method,
        Uri url,
        ValidationSessionRole role,
        string? authorizationHeaderValue,
        CancellationToken cancellationToken = default)
    {
        if (StopRequested || run.StopRequested)
        {
            throw new InvalidOperationException("Validation stopped by user.");
        }

        if (run.ActualRequestCount >= run.MaxRequestCount)
        {
            throw new InvalidOperationException("MaxRequestCount exceeded.");
        }

        var allowed = method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Options;
        if (!allowed)
        {
            throw new InvalidOperationException("State-changing HTTP methods are blocked by default.");
        }

        if (_options.Value.DelayBetweenRequestsMs > 0 && run.ActualRequestCount > 0)
        {
            await Task.Delay(_options.Value.DelayBetweenRequestsMs, cancellationToken);
        }

        using var client = _httpFactory.Create(TimeSpan.FromSeconds(12), maxRedirects: 0, allowRedirects: false);
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(_options.Value.UserAgent);

        using var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrWhiteSpace(authorizationHeaderValue))
        {
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeaderValue);
        }

        var chain = new List<string>();
        var current = url;
        var status = 0;
        string? contentType = null;
        var body = string.Empty;
        var final = url.ToString();

        for (var hop = 0; hop < 5; hop++)
        {
            request.RequestUri = current;
            using var response = await client.SendAsync(request, cancellationToken);
            run.ActualRequestCount++;
            status = (int)response.StatusCode;
            final = response.RequestMessage?.RequestUri?.ToString() ?? current.ToString();
            contentType = response.Content.Headers.ContentType?.ToString();

            if (status == 429)
            {
                _logger.LogWarning("Validation hit 429 for {Url}", current);
                return new ValidationHttpResponse(status, final, chain, contentType, string.Empty, true, false);
            }

            if (status >= 500)
            {
                _serverErrors++;
                if (_serverErrors >= 3)
                {
                    return new ValidationHttpResponse(status, final, chain, contentType, string.Empty, false, true);
                }
            }

            if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is { } loc)
            {
                var next = loc.IsAbsoluteUri ? loc : new Uri(current, loc);
                chain.Add($"{current} → {status} → {next}");
                current = next;
                continue;
            }

            if (method != HttpMethod.Head)
            {
                body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (body.Length > 64_000)
                {
                    body = body[..64_000];
                }
            }

            break;
        }

        // Never attempt bypass after 401/403.
        return new ValidationHttpResponse(status, final, chain, contentType, body, false, false);
    }
}
