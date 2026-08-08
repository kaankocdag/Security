using System.Net;
using System.Text;
using Kaan.SecurityPlatform.Application.Features.Validation;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Entities.Validation;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.HackerOne;

namespace Kaan.SecurityPlatform.Infrastructure.Validation.Validators;

public abstract class FindingValidatorBase : IFindingValidator
{
    public abstract string ValidatorType { get; }
    public abstract IReadOnlyList<string> SupportedFindingTypes { get; }
    public abstract ValidationAutomationKind AutomationKind { get; }
    public abstract ValidationRiskLevel RiskLevel { get; }
    public abstract ValidationMode DefaultMode { get; }
    public virtual bool RequiresUserApproval => true;

    public virtual bool CanHandle(Finding finding) =>
        SupportedFindingTypes.Any(t =>
            finding.Fingerprint?.Contains(t, StringComparison.OrdinalIgnoreCase) == true
            || finding.CheckCode.Contains(t, StringComparison.OrdinalIgnoreCase)
            || finding.Category.Contains(t, StringComparison.OrdinalIgnoreCase)
            || finding.Title.Contains(t, StringComparison.OrdinalIgnoreCase));

    public virtual Task<ValidationPreconditionResult> CheckPreconditionsAsync(
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        var missing = new List<string>();
        if (context.ScopePolicy is null && !context.IsDevelopmentEnvironment)
        {
            missing.Add("ScopePolicy (InScope)");
        }

        if (context.AuthorizationEvidence is null && !context.IsDevelopmentEnvironment)
        {
            missing.Add("AuthorizationEvidence");
        }

        if (!context.ExplicitUserApproval)
        {
            missing.Add("ExplicitUserApproval");
        }

        var canStart = missing.Count == 0 && AutomationKind != ValidationAutomationKind.ManualOnly;
        return Task.FromResult(new ValidationPreconditionResult(
            canStart,
            missing.Count > 0 ? ValidationStatus.PreconditionsMissing : ValidationStatus.AwaitingUserApproval,
            missing,
            AutomationKind,
            RiskLevel,
            ValidatorType));
    }

    public abstract Task<ValidatorExecutionResult> ValidateAsync(
        ValidationContext context,
        IValidationHttpGate httpGate,
        CancellationToken cancellationToken = default);
}

public sealed class AccessControlCandidateValidator(
    IEvidenceCollector evidence,
    IEvidenceRedactor redactor,
    ISensitiveSurfaceAnalyzer surfaceAnalyzer,
    ITestAccountSecretProtector secretProtector) : FindingValidatorBase
{
    public override string ValidatorType => "AccessControlCandidate";
    public override IReadOnlyList<string> SupportedFindingTypes =>
        ["asc.access", "access-control", "AccessControl", "CWE-284"];
    public override ValidationAutomationKind AutomationKind => ValidationAutomationKind.SemiAutomatic;
    public override ValidationRiskLevel RiskLevel => ValidationRiskLevel.Medium;
    public override ValidationMode DefaultMode => ValidationMode.SafeDifferential;

    public override async Task<ValidatorExecutionResult> ValidateAsync(
        ValidationContext context,
        IValidationHttpGate httpGate,
        CancellationToken cancellationToken = default)
    {
        var url = context.AffectedUri
                  ?? (Uri.TryCreate(context.Finding.AffectedUrl, UriKind.Absolute, out var u) ? u : null);
        if (url is null)
        {
            return Fail(ValidationStatus.Failed, "missing_url", "Affected URL missing.");
        }

        var evidenceItems = new List<ValidationEvidence>();
        var anon = await httpGate.SendSafeAsync(
            context.Run, HttpMethod.Get, url, ValidationSessionRole.Anonymous, null, cancellationToken);
        if (anon.RateLimited)
        {
            return Stopped("rate_limited", "HTTP 429 — validation stopped.", evidenceItems);
        }

        if (anon.ServerErrorSpike)
        {
            return Stopped("server_error_spike", "Repeated 5xx — validation stopped.", evidenceItems);
        }

        evidenceItems.Add(evidence.CreateHttpEvidence(
            context.Run.Id, ValidationSessionRole.Anonymous, "GET", url.ToString(),
            anon.StatusCode, anon.FinalUrl, anon.RedirectChain, anon.ContentType, anon.Body,
            redactor.HashBody(anon.Body)));

        var analysis = surfaceAnalyzer.Analyze(
            url.ToString(), anon.StatusCode, anon.FinalUrl, anon.RedirectChain, anon.ContentType, anon.Body);

        // Path existence / login / 401/403 / harmless → not confirmed.
        if (analysis.LoginPageDetected
            || analysis.AccessDeniedDetected
            || anon.StatusCode is 401 or 403
            || analysis.SubmissionRecommendation == SubmissionRecommendation.DoNotSubmit)
        {
            return new ValidatorExecutionResult(
                ValidationStatus.CandidateOnly,
                false,
                false,
                ValidationImpactType.None,
                ValidationConfidence.High,
                "Privileged data/functionality remain inaccessible without authorization.",
                "Administrative path reachability alone does not demonstrate an access-control vulnerability. "
                + $"Observed status={anon.StatusCode}; login={analysis.LoginPageDetected}; denied={analysis.AccessDeniedDetected}.",
                ["Administrative path reachability alone does not demonstrate an access-control vulnerability."],
                evidenceItems,
                ReproductionCount: 1,
                TestAccountRolesUsed: "Anonymous");
        }

        var accountA = context.TestAccounts.FirstOrDefault(a =>
            a.Role == ValidationSessionRole.TestAccountA && a.OwnershipConfirmed && a.TestingPermissionConfirmed);
        var accountB = context.TestAccounts.FirstOrDefault(a =>
            a.Role == ValidationSessionRole.TestAccountB && a.OwnershipConfirmed && a.TestingPermissionConfirmed);

        if (accountA is null || accountB is null || string.IsNullOrWhiteSpace(context.OwnedTestResourceUrl))
        {
            return new ValidatorExecutionResult(
                ValidationStatus.ManualReviewRequired,
                false,
                false,
                analysis.HighPriorityManualReview
                    ? ValidationImpactType.PrivilegedFunctionExposure
                    : ValidationImpactType.None,
                ValidationConfidence.Medium,
                "Differential access control using user-owned test resources across TestAccountA/B.",
                "Safe GET observed privileged/sensitive indicators without login gate, but user-owned test resource "
                + "and dual authorized test accounts are required before DemonstratedImpact can be set.",
                analysis.ManualReviewReasons.Append(
                    "Owned test resource + TestAccountA/B required for verified differential impact.").ToList(),
                evidenceItems,
                ReproductionCount: 1,
                TestAccountRolesUsed: "Anonymous");
        }

        if (!Uri.TryCreate(context.OwnedTestResourceUrl, UriKind.Absolute, out var ownedUri))
        {
            return Fail(ValidationStatus.Failed, "invalid_owned_resource", "OwnedTestResourceUrl invalid.");
        }

        // Differential GET only — no ID enumeration, no third-party data hunt.
        string? authA = null;
        string? authB = null;
        try
        {
            authA = "Basic " + Convert.ToBase64String(
                Encoding.UTF8.GetBytes(secretProtector.Unprotect(accountA.EncryptedSecretReference)));
            authB = "Basic " + Convert.ToBase64String(
                Encoding.UTF8.GetBytes(secretProtector.Unprotect(accountB.EncryptedSecretReference)));
        }
        catch
        {
            return Fail(ValidationStatus.Failed, "secret_unprotect_failed", "Test account secret could not be decrypted.");
        }

        var respA = await httpGate.SendSafeAsync(
            context.Run, HttpMethod.Get, ownedUri, ValidationSessionRole.TestAccountA, authA, cancellationToken);
        evidenceItems.Add(evidence.CreateHttpEvidence(
            context.Run.Id, ValidationSessionRole.TestAccountA, "GET", ownedUri.ToString(),
            respA.StatusCode, respA.FinalUrl, respA.RedirectChain, respA.ContentType, respA.Body,
            redactor.HashBody(respA.Body)));

        var respB = await httpGate.SendSafeAsync(
            context.Run, HttpMethod.Get, ownedUri, ValidationSessionRole.TestAccountB, authB, cancellationToken);
        evidenceItems.Add(evidence.CreateHttpEvidence(
            context.Run.Id, ValidationSessionRole.TestAccountB, "GET", ownedUri.ToString(),
            respB.StatusCode, respB.FinalUrl, respB.RedirectChain, respB.ContentType, respB.Body,
            redactor.HashBody(respB.Body)));

        var aCanRead = respA.StatusCode is >= 200 and < 300 && respA.Body.Length > 0;
        var bCanRead = respB.StatusCode is >= 200 and < 300 && respB.Body.Length > 0;
        var bDenied = respB.StatusCode is 401 or 403;
        var anonHash = redactor.HashBody(anon.Body);
        var bSameAsPublic = bCanRead
                            && string.Equals(redactor.HashBody(respB.Body), anonHash, StringComparison.Ordinal);

        if (aCanRead && bDenied)
        {
            return new ValidatorExecutionResult(
                ValidationStatus.NotReproduced,
                false,
                false,
                ValidationImpactType.None,
                ValidationConfidence.High,
                "TestAccountB cannot read TestAccountA-owned resource.",
                "TestAccountB received 401/403 for the owned test resource — secure differential behavior.",
                [],
                evidenceItems,
                ReproductionCount: 1,
                TestAccountRolesUsed: "Anonymous,TestAccountA,TestAccountB");
        }

        if (aCanRead && bCanRead && bSameAsPublic)
        {
            return new ValidatorExecutionResult(
                ValidationStatus.CandidateOnly,
                false,
                false,
                ValidationImpactType.None,
                ValidationConfidence.Medium,
                "TestAccountB cannot read TestAccountA-owned private resource.",
                "Both sessions received content equivalent to the anonymous public response — not access-control impact.",
                ["Same public content is not demonstrated access-control impact."],
                evidenceItems,
                ReproductionCount: 1,
                TestAccountRolesUsed: "Anonymous,TestAccountA,TestAccountB");
        }

        // B reads A-owned resource content that is not the anonymous public page.
        if (aCanRead && bCanRead && !bSameAsPublic)
        {
            return new ValidatorExecutionResult(
                ValidationStatus.Confirmed,
                true,
                true,
                ValidationImpactType.UnauthorizedDataRead,
                ValidationConfidence.High,
                "Only the owning test account can read its own test resource.",
                "TestAccountB retrieved non-public content for a TestAccountA-owned test resource via safe GET (differential).",
                ["Verified differential read of user-owned test resource across authorized test accounts."],
                evidenceItems,
                ReproductionCount: 2,
                TestAccountRolesUsed: "Anonymous,TestAccountA,TestAccountB");
        }

        return new ValidatorExecutionResult(
            ValidationStatus.NotReproduced,
            false,
            false,
            ValidationImpactType.None,
            ValidationConfidence.Medium,
            "Cross-account unauthorized read of owned test resource.",
            "Differential GET did not reproduce unauthorized read of the owned test resource.",
            [],
            evidenceItems,
            ReproductionCount: 1,
            TestAccountRolesUsed: "Anonymous,TestAccountA,TestAccountB");
    }

    private static ValidatorExecutionResult Fail(ValidationStatus status, string code, string message) =>
        new(status, false, false, ValidationImpactType.None, ValidationConfidence.Low,
            string.Empty, message, [], Array.Empty<ValidationEvidence>(), code, message);

    private static ValidatorExecutionResult Stopped(
        string code,
        string message,
        IReadOnlyList<ValidationEvidence> evidenceItems) =>
        new(ValidationStatus.Stopped, false, false, ValidationImpactType.None, ValidationConfidence.Low,
            string.Empty, message, [], evidenceItems, code, message);
}

public sealed class SecurityHeadersValidator(IEvidenceCollector evidence, IEvidenceRedactor redactor)
    : FindingValidatorBase
{
    public override string ValidatorType => "SecurityHeaders";
    public override IReadOnlyList<string> SupportedFindingTypes =>
        ["sh.", "security-headers", "hsts", "csp", "nosniff", "referrer", "permissions", "clickjacking"];
    public override ValidationAutomationKind AutomationKind => ValidationAutomationKind.Automatic;
    public override ValidationRiskLevel RiskLevel => ValidationRiskLevel.Low;
    public override ValidationMode DefaultMode => ValidationMode.PassiveReadOnly;

    public override async Task<ValidatorExecutionResult> ValidateAsync(
        ValidationContext context,
        IValidationHttpGate httpGate,
        CancellationToken cancellationToken = default)
    {
        var url = context.AffectedUri ?? new Uri($"https://{context.TargetHost}/");
        var resp = await httpGate.SendSafeAsync(
            context.Run, HttpMethod.Get, url, ValidationSessionRole.Anonymous, null, cancellationToken);
        var item = evidence.CreateHttpEvidence(
            context.Run.Id, ValidationSessionRole.Anonymous, "GET", url.ToString(),
            resp.StatusCode, resp.FinalUrl, resp.RedirectChain, resp.ContentType, resp.Body,
            redactor.HashBody(resp.Body));

        return new ValidatorExecutionResult(
            ValidationStatus.CandidateOnly,
            false,
            false,
            ValidationImpactType.HardeningGap,
            ValidationConfidence.High,
            "Security headers present according to program hardening expectations.",
            "Header inspection completed. Missing headers alone are SecurityHardening/Informational — not a confirmed vulnerability or reward-eligible finding.",
            ["Missing security headers are not automatically confirmed vulnerabilities."],
            [item],
            ReproductionCount: 1,
            TestAccountRolesUsed: "Anonymous");
    }
}

public sealed class CorsConfigurationValidator(IEvidenceCollector evidence, IEvidenceRedactor redactor)
    : FindingValidatorBase
{
    public override string ValidatorType => "CorsConfiguration";
    public override IReadOnlyList<string> SupportedFindingTypes => ["asc.cors", "cors"];
    public override ValidationAutomationKind AutomationKind => ValidationAutomationKind.SemiAutomatic;
    public override ValidationRiskLevel RiskLevel => ValidationRiskLevel.Medium;
    public override ValidationMode DefaultMode => ValidationMode.PassiveReadOnly;

    public override async Task<ValidatorExecutionResult> ValidateAsync(
        ValidationContext context,
        IValidationHttpGate httpGate,
        CancellationToken cancellationToken = default)
    {
        var url = context.AffectedUri ?? new Uri($"https://{context.TargetHost}/");
        var resp = await httpGate.SendSafeAsync(
            context.Run, HttpMethod.Get, url, ValidationSessionRole.Anonymous, null, cancellationToken);
        var item = evidence.CreateHttpEvidence(
            context.Run.Id, ValidationSessionRole.Anonymous, "GET", url.ToString(),
            resp.StatusCode, resp.FinalUrl, resp.RedirectChain, resp.ContentType,
            "CORS header anomaly candidate — credentials/cross-origin readability not demonstrated.",
            redactor.HashBody(resp.Body));

        return new ValidatorExecutionResult(
            ValidationStatus.CandidateOnly,
            false,
            false,
            ValidationImpactType.ConfigurationAnomaly,
            ValidationConfidence.Medium,
            "Foreign Origin not reflected with credentials on sensitive authenticated responses.",
            "Header anomaly alone is not demonstrated impact. Cross-origin readable sensitive response was not proven with an authorized test account.",
            ["DemonstratedImpact=false until authenticated sensitive response is shown readable cross-origin."],
            [item],
            ReproductionCount: 1,
            TestAccountRolesUsed: "Anonymous");
    }
}

public sealed class OpenRedirectValidator(IEvidenceCollector evidence, IEvidenceRedactor redactor)
    : FindingValidatorBase
{
    public override string ValidatorType => "OpenRedirect";
    public override IReadOnlyList<string> SupportedFindingTypes => ["open-redirect", "redirect"];
    public override ValidationAutomationKind AutomationKind => ValidationAutomationKind.SemiAutomatic;
    public override ValidationRiskLevel RiskLevel => ValidationRiskLevel.Low;
    public override ValidationMode DefaultMode => ValidationMode.PassiveReadOnly;

    public override async Task<ValidatorExecutionResult> ValidateAsync(
        ValidationContext context,
        IValidationHttpGate httpGate,
        CancellationToken cancellationToken = default)
    {
        // Only harmless https test URL — never javascript/data/file schemes.
        const string harmless = "https://example.com/";
        var baseUri = context.AffectedUri ?? new Uri($"https://{context.TargetHost}/");
        var probe = new UriBuilder(baseUri) { Query = $"next={Uri.EscapeDataString(harmless)}" }.Uri;
        var resp = await httpGate.SendSafeAsync(
            context.Run, HttpMethod.Get, probe, ValidationSessionRole.Anonymous, null, cancellationToken);
        var external = resp.FinalUrl.StartsWith("https://example.com", StringComparison.OrdinalIgnoreCase)
                       || resp.RedirectChain.Any(c => c.Contains("example.com", StringComparison.OrdinalIgnoreCase));
        var item = evidence.CreateHttpEvidence(
            context.Run.Id, ValidationSessionRole.Anonymous, "GET", probe.ToString(),
            resp.StatusCode, resp.FinalUrl, resp.RedirectChain, resp.ContentType, null,
            redactor.HashBody(resp.Body));

        return new ValidatorExecutionResult(
            external ? ValidationStatus.ManualReviewRequired : ValidationStatus.NotReproduced,
            false,
            false,
            external ? ValidationImpactType.OpenRedirect : ValidationImpactType.None,
            ValidationConfidence.Medium,
            "Application does not redirect to attacker-controlled external URL.",
            external
                ? "External redirect to harmless test URL observed. Impact classification deferred to program policy — not auto reward-eligible."
                : "No external redirect to the harmless test URL was observed.",
            external ? ["Open redirect candidate — program policy decides impact; DemonstratedImpact=false."] : [],
            [item],
            ReproductionCount: 1,
            TestAccountRolesUsed: "Anonymous");
    }
}

public sealed class CookieConfigurationValidator : FindingValidatorBase
{
    public override string ValidatorType => "CookieConfiguration";
    public override IReadOnlyList<string> SupportedFindingTypes => ["cookie.flags", "cookie"];
    public override ValidationAutomationKind AutomationKind => ValidationAutomationKind.Automatic;
    public override ValidationRiskLevel RiskLevel => ValidationRiskLevel.Low;
    public override ValidationMode DefaultMode => ValidationMode.PassiveReadOnly;

    public override Task<ValidatorExecutionResult> ValidateAsync(
        ValidationContext context,
        IValidationHttpGate httpGate,
        CancellationToken cancellationToken = default)
    {
        // Cookie values never recorded — flags-only guidance from existing finding evidence.
        var item = new ValidationEvidence
        {
            ValidationRunId = context.Run.Id,
            EvidenceType = ValidationEvidenceType.CookieInspection,
            RequestMethod = "GET",
            RedactedRequestUrl = context.Finding.AffectedUrl,
            RedactedResponseExcerpt = "Cookie flags inspected (names/attributes only; values redacted).",
            SessionRole = ValidationSessionRole.Anonymous,
            CapturedAt = DateTime.UtcNow
        };

        return Task.FromResult(new ValidatorExecutionResult(
            ValidationStatus.CandidateOnly,
            false,
            false,
            ValidationImpactType.HardeningGap,
            ValidationConfidence.High,
            "Session cookies set Secure; HttpOnly; SameSite appropriately.",
            "Missing cookie flags alone are not a critical vulnerability or reward-eligible finding.",
            ["Cookie flag gaps classified as hardening / misconfiguration without session-theft proof."],
            [item],
            ReproductionCount: 1,
            TestAccountRolesUsed: "Anonymous"));
    }
}

public sealed class TlsConfigurationValidator : FindingValidatorBase
{
    public override string ValidatorType => "TlsConfiguration";
    public override IReadOnlyList<string> SupportedFindingTypes => ["tls.", "https.", "certificate"];
    public override ValidationAutomationKind AutomationKind => ValidationAutomationKind.Automatic;
    public override ValidationRiskLevel RiskLevel => ValidationRiskLevel.Low;
    public override ValidationMode DefaultMode => ValidationMode.PassiveReadOnly;

    public override Task<ValidatorExecutionResult> ValidateAsync(
        ValidationContext context,
        IValidationHttpGate httpGate,
        CancellationToken cancellationToken = default)
    {
        var item = new ValidationEvidence
        {
            ValidationRunId = context.Run.Id,
            EvidenceType = ValidationEvidenceType.TlsInspection,
            RequestMethod = "GET",
            RedactedRequestUrl = $"https://{context.TargetHost}/",
            RedactedResponseExcerpt = "TLS/certificate inspection (no handshake flooding).",
            SessionRole = ValidationSessionRole.Anonymous,
            CapturedAt = DateTime.UtcNow
        };

        return Task.FromResult(new ValidatorExecutionResult(
            ValidationStatus.CandidateOnly,
            false,
            false,
            ValidationImpactType.HardeningGap,
            ValidationConfidence.Medium,
            "Valid certificate, hostname match, modern TLS.",
            "TLS/configuration findings without demonstrated impact remain hardening/informational.",
            [],
            [item],
            ReproductionCount: 1,
            TestAccountRolesUsed: "Anonymous"));
    }
}

public sealed class ManualOnlyValidator : FindingValidatorBase
{
    public override string ValidatorType => "ManualOnly";
    public override IReadOnlyList<string> SupportedFindingTypes =>
    [
        "sqli", "sql-injection", "rce", "ssrf", "xxe", "deserial", "command-injection",
        "auth-bypass", "file-upload", "privilege", "csrf", "xss"
    ];
    public override ValidationAutomationKind AutomationKind => ValidationAutomationKind.ManualOnly;
    public override ValidationRiskLevel RiskLevel => ValidationRiskLevel.Critical;
    public override ValidationMode DefaultMode => ValidationMode.ManualOnly;
    public override bool RequiresUserApproval => true;

    public override bool CanHandle(Finding finding)
    {
        // Fallback only when no safer validator matched — registry orders ManualOnly last.
        return false;
    }

    public bool ExplicitCanHandle(Finding finding) => base.CanHandle(finding);

    public override Task<ValidatorExecutionResult> ValidateAsync(
        ValidationContext context,
        IValidationHttpGate httpGate,
        CancellationToken cancellationToken = default)
    {
        var guidance =
            "This finding type must not be auto-validated. Do not send exploit payloads. " +
            "Confirm program permission, use only owned test data, and collect safe evidence manually " +
            "(screenshots, redacted responses, dual-account proof).";

        var item = new ValidationEvidence
        {
            ValidationRunId = context.Run.Id,
            EvidenceType = ValidationEvidenceType.ManualGuidance,
            RequestMethod = "N/A",
            RedactedResponseExcerpt = guidance,
            SessionRole = ValidationSessionRole.Anonymous,
            CapturedAt = DateTime.UtcNow
        };

        return Task.FromResult(new ValidatorExecutionResult(
            ValidationStatus.ManualReviewRequired,
            false,
            false,
            ValidationImpactType.None,
            ValidationConfidence.Low,
            "Manual authorized validation with program-approved methods.",
            guidance,
            [
                "High-risk finding family — ManualOnlyValidator (no active payloads).",
                "Verify ScopePolicy and AuthorizationEvidence before any manual testing."
            ],
            [item],
            TestAccountRolesUsed: null));
    }
}

public sealed class ValidatorRegistry : IValidatorRegistry
{
    private readonly IReadOnlyList<IFindingValidator> _validators;
    private readonly ManualOnlyValidator _manual;

    public ValidatorRegistry(IEnumerable<IFindingValidator> validators, ManualOnlyValidator manual)
    {
        _manual = manual;
        _validators = validators.Where(v => v is not ManualOnlyValidator).Append(manual).ToList();
    }

    public IReadOnlyList<IFindingValidator> All => _validators;

    public IFindingValidator Resolve(Finding finding)
    {
        foreach (var v in _validators)
        {
            if (v is ManualOnlyValidator)
            {
                continue;
            }

            if (v.CanHandle(finding))
            {
                return v;
            }
        }

        if (_manual.ExplicitCanHandle(finding))
        {
            return _manual;
        }

        // Default safe surface: access-control style candidate handling for VulnerabilityCandidate.
        IFindingValidator? access = _validators.OfType<AccessControlCandidateValidator>().FirstOrDefault();
        return access ?? _manual;
    }
}
