using Kaan.SecurityPlatform.Application.Features.BugBounty;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Infrastructure.BugBounty;

/// <summary>
/// Fingerprint / check koduna göre FindingClass, TechnicalSeverity ve eligibility üretir.
/// Scanner Severity korunur; TechnicalSeverity bağımsızdır.
/// </summary>
public sealed class FindingValidationClassifier : IFindingValidationClassifier
{
    private readonly IReadOnlyDictionary<string, IBugBountyProgramPolicy> _policies;

    public FindingValidationClassifier(IEnumerable<IBugBountyProgramPolicy> policies)
    {
        _policies = policies.ToDictionary(p => p.PolicyKey, StringComparer.OrdinalIgnoreCase);
    }

    public void Classify(Finding finding, string programPolicyKey = AmazonVrpPolicy.PolicyKeyConstant)
    {
        var profile = ResolveProfile(finding.Fingerprint, finding.CheckCode, finding.Severity);
        finding.FindingClass = profile.FindingClass;
        finding.TechnicalSeverity = profile.TechnicalSeverity;
        finding.Exploitability = profile.Exploitability;
        finding.DemonstratedImpact = profile.DemonstratedImpact;
        finding.RequiresManualValidation = profile.RequiresManualValidation;
        finding.PolicyCategory = profile.PolicyCategory;

        if (!_policies.TryGetValue(programPolicyKey, out var policy))
        {
            policy = _policies.Values.FirstOrDefault()
                     ?? new AmazonVrpPolicy();
            programPolicyKey = policy.PolicyKey;
        }

        finding.ProgramPolicyMatch = programPolicyKey;
        finding.SubmissionRecommendation = policy.Evaluate(profile.PolicyCategory, profile.DemonstratedImpact);

        ApplyXssCandidateRules(finding, profile);
        ApplyAccessControlCandidateRules(finding, profile);

        finding.BugBountyEligible =
            finding.SubmissionRecommendation is SubmissionRecommendation.Submit or SubmissionRecommendation.ManualReview
            && profile.DemonstratedImpact
            && profile.FindingClass == FindingClass.Vulnerability;

        // Amazon VRP: ManualReview için demonstrated impact olan misconfig'ler aday listesine girebilir
        if (!finding.BugBountyEligible
            && finding.SubmissionRecommendation == SubmissionRecommendation.ManualReview
            && profile.DemonstratedImpact
            && profile.FindingClass is FindingClass.Vulnerability or FindingClass.SecurityMisconfiguration)
        {
            finding.BugBountyEligible = true;
        }

        // VulnerabilityCandidate asla otomatik eligible/Submit değil (impact kanıtlanmadıkça)
        if (finding.FindingClass == FindingClass.VulnerabilityCandidate && !finding.DemonstratedImpact)
        {
            finding.BugBountyEligible = false;
            if (finding.SubmissionRecommendation == SubmissionRecommendation.Submit)
            {
                finding.SubmissionRecommendation = SubmissionRecommendation.ManualReview;
            }
        }

        finding.EligibilityReason = BuildReason(finding, profile);
        if (!string.IsNullOrEmpty(profile.EligibilityOverride))
        {
            finding.EligibilityReason = profile.EligibilityOverride;
        }
    }

    private static void ApplyXssCandidateRules(Finding finding, ClassificationProfile profile)
    {
        if (profile.PolicyCategory != BugBountyPolicyCategory.Xss
            && finding.Fingerprint is not "asc.xss.reflected-marker")
        {
            return;
        }

        finding.FindingClass = FindingClass.VulnerabilityCandidate;
        finding.BugBountySeverity = BugBountySeverity.Unassigned;
        finding.TechnicalPotentialSeverity = Severity.Medium;
        finding.TechnicalSeverity = Severity.Informational;
        finding.DemonstratedImpact = false;
        finding.RequiresManualValidation = true;

        var properlyEncoded = finding.HtmlEncoded == true
                              || finding.AttributeEncoded == true;

        if (properlyEncoded)
        {
            finding.SubmissionRecommendation = SubmissionRecommendation.DoNotSubmit;
            finding.BugBountyEligible = false;
            profile.EligibilityOverride =
                "Properly encoded reflected input; no XSS impact.";
            return;
        }

        if (finding.ReflectionContext is null or ReflectionContext.Unknown)
        {
            finding.SubmissionRecommendation = SubmissionRecommendation.ManualReview;
            profile.EligibilityOverride =
                "Reflection context unclear; browser-side execution not demonstrated — Manual Review only.";
            return;
        }

        // Raw reflection in a known context still is NOT confirmed XSS
        finding.SubmissionRecommendation = SubmissionRecommendation.ManualReview;
        profile.EligibilityOverride =
            "Reflected input candidate only; Confirmed Vulnerability=No, Demonstrated Impact=No — never auto-Submit.";
    }

    private static void ApplyAccessControlCandidateRules(Finding finding, ClassificationProfile profile)
    {
        var fp = finding.Fingerprint ?? string.Empty;
        var isAccess = fp.StartsWith("asc.access.", StringComparison.OrdinalIgnoreCase)
                       || profile.PolicyCategory == BugBountyPolicyCategory.PrivilegeEscalation;
        if (!isAccess)
        {
            return;
        }

        // Verified unauthorized privileged access → Confirmed Vulnerability.
        if (fp.Equals("asc.access.confirmed-unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            finding.FindingClass = FindingClass.Vulnerability;
            finding.BugBountySeverity = BugBountySeverity.High;
            finding.TechnicalPotentialSeverity = Severity.High;
            finding.TechnicalSeverity = Severity.High;
            finding.DemonstratedImpact = true;
            finding.RequiresManualValidation = false;
            finding.SubmissionRecommendation = SubmissionRecommendation.Submit;
            finding.BugBountyEligible = true;
            finding.Exploitability = Exploitability.Demonstrated;
            profile.EligibilityOverride =
                "Confirmed Vulnerability: verified unauthorized privileged access demonstrated. Submit eligible.";
            return;
        }

        finding.BugBountySeverity = BugBountySeverity.Unassigned;
        finding.TechnicalPotentialSeverity = Severity.Medium;
        finding.TechnicalSeverity = Severity.Informational;
        finding.DemonstratedImpact = false;
        finding.BugBountyEligible = false;

        // Safe surface (login / 403 / public) — Informational, never Vulnerability.
        if (fp.Equals("asc.access.surface-donotsubmit", StringComparison.OrdinalIgnoreCase))
        {
            finding.FindingClass = FindingClass.Informational;
            finding.RequiresManualValidation = false;
            finding.SubmissionRecommendation = SubmissionRecommendation.DoNotSubmit;
            profile.EligibilityOverride =
                "Sensitive-looking path is reachable, but no unauthorized privileged access " +
                "or sensitive data exposure was demonstrated. Path existence alone is not a vulnerability. DoNotSubmit.";
            return;
        }

        var isManualSurface = fp.Equals("asc.access.surface-manualreview", StringComparison.OrdinalIgnoreCase)
                              || fp.Equals("asc.access.surface-manualreview-high", StringComparison.OrdinalIgnoreCase);

        // ManualReview only when analyzer recorded concrete ManualReviewReasons.
        if (isManualSurface && !EvidenceHasManualReviewReasons(finding.Evidence))
        {
            finding.FindingClass = FindingClass.Informational;
            finding.RequiresManualValidation = false;
            finding.SubmissionRecommendation = SubmissionRecommendation.DoNotSubmit;
            finding.Fingerprint = "asc.access.surface-donotsubmit";
            profile.EligibilityOverride =
                "ManualReviewReasons empty — downgraded to Informational/DoNotSubmit. Path existence alone is not a vulnerability.";
            return;
        }

        // Privileged UI or sensitive-data indicators — candidate only (not confirmed).
        finding.FindingClass = FindingClass.VulnerabilityCandidate;
        finding.RequiresManualValidation = true;
        finding.SubmissionRecommendation = SubmissionRecommendation.ManualReview;
        if (fp.Equals("asc.access.surface-manualreview-high", StringComparison.OrdinalIgnoreCase))
        {
            finding.TechnicalPotentialSeverity = Severity.High;
            profile.EligibilityOverride =
                "High-priority AccessControlCandidate: unauthenticated sensitive-data indicators. " +
                "ConfirmedVulnerability=false until unauthorized privileged access is verified. Manual Review — never auto-Submit.";
            return;
        }

        profile.EligibilityOverride =
            "AccessControlCandidate: unvalidated. Path reachability alone is not a vulnerability. " +
            "No unauthorized access to privileged data or functionality has been demonstrated. Manual Review only — never auto-Submit.";
    }

    private static bool EvidenceHasManualReviewReasons(string? evidence)
    {
        if (string.IsNullOrWhiteSpace(evidence))
        {
            return false;
        }

        if (evidence.Contains("ManualReviewReasons: (none)", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var idx = evidence.IndexOf("ManualReviewReasons:", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return false;
        }

        var slice = evidence[idx..];
        return slice.Contains("\n- ", StringComparison.Ordinal);
    }

    private static ClassificationProfile ResolveProfile(string? fingerprint, string checkCode, Severity scannerSeverity)
    {
        var fp = fingerprint ?? string.Empty;

        if (fp.StartsWith("sh.hsts.", StringComparison.OrdinalIgnoreCase)
            || fp.Equals("sh.csp.missing", StringComparison.OrdinalIgnoreCase)
            || fp.Equals("sh.csp.weak", StringComparison.OrdinalIgnoreCase)
            || fp.Equals("sh.nosniff.missing", StringComparison.OrdinalIgnoreCase))
        {
            return new ClassificationProfile(
                FindingClass.HardeningRecommendation,
                Cap(scannerSeverity, Severity.Low),
                Exploitability.None,
                DemonstratedImpact: false,
                RequiresManualValidation: true,
                BugBountyPolicyCategory.MissingSecurityHeaders,
                "Missing security header; no exploitability / demonstrated impact on its own.");
        }

        if (fp.Equals("sh.permissions.missing", StringComparison.OrdinalIgnoreCase)
            || fp.Equals("sh.referrer.missing", StringComparison.OrdinalIgnoreCase))
        {
            return new ClassificationProfile(
                FindingClass.Informational,
                Severity.Informational,
                Exploitability.None,
                DemonstratedImpact: false,
                RequiresManualValidation: false,
                BugBountyPolicyCategory.MissingSecurityHeaders,
                "Missing policy header; informational / hardening only.");
        }

        if (fp.Equals("sh.clickjacking.missing", StringComparison.OrdinalIgnoreCase))
        {
            return new ClassificationProfile(
                FindingClass.HardeningRecommendation,
                Cap(scannerSeverity, Severity.Low),
                Exploitability.Theoretical,
                DemonstratedImpact: false,
                RequiresManualValidation: true,
                BugBountyPolicyCategory.Clickjacking,
                "Missing clickjacking header; not a vulnerability without frameability + sensitive action proof.");
        }

        if (fp.StartsWith("cookie.flags.", StringComparison.OrdinalIgnoreCase))
        {
            return new ClassificationProfile(
                FindingClass.SecurityMisconfiguration,
                Cap(scannerSeverity, Severity.Medium),
                Exploitability.Theoretical,
                DemonstratedImpact: false,
                RequiresManualValidation: true,
                BugBountyPolicyCategory.MissingCookieFlags,
                "Missing cookie flags; not bug-bounty eligible without session-theft proof.");
        }

        if (fp.Equals("wellknown.sitemap.missing", StringComparison.OrdinalIgnoreCase)
            || fp.Equals("wellknown.robots.missing", StringComparison.OrdinalIgnoreCase))
        {
            return new ClassificationProfile(
                FindingClass.SeoIssue,
                Severity.Informational,
                Exploitability.None,
                DemonstratedImpact: false,
                RequiresManualValidation: false,
                BugBountyPolicyCategory.ScannerOutputOnly,
                "SEO / discoverability issue; not a security vulnerability for bug bounty.");
        }

        if (fp.Equals("wellknown.security-txt.missing", StringComparison.OrdinalIgnoreCase))
        {
            return new ClassificationProfile(
                FindingClass.ComplianceIssue,
                Severity.Informational,
                Exploitability.None,
                DemonstratedImpact: false,
                RequiresManualValidation: false,
                BugBountyPolicyCategory.ScannerOutputOnly,
                "Missing security.txt; compliance/process issue, not a BB vulnerability.");
        }

        if (fp.StartsWith("info.", StringComparison.OrdinalIgnoreCase)
            || fp.Equals("info.error-leak", StringComparison.OrdinalIgnoreCase)
            || fp.Equals("asc.info.verbose-error", StringComparison.OrdinalIgnoreCase))
        {
            return new ClassificationProfile(
                FindingClass.Informational,
                Cap(scannerSeverity, Severity.Low),
                Exploitability.Theoretical,
                DemonstratedImpact: false,
                RequiresManualValidation: true,
                BugBountyPolicyCategory.InformationDisclosure,
                "Information disclosure indicator; Manual Review without sensitive-data proof.");
        }

        if (fp.Equals("asc.xss.reflected-marker", StringComparison.OrdinalIgnoreCase))
        {
            return new ClassificationProfile(
                FindingClass.VulnerabilityCandidate,
                Severity.Informational,
                Exploitability.Theoretical,
                DemonstratedImpact: false,
                RequiresManualValidation: true,
                BugBountyPolicyCategory.Xss,
                "Single harmless marker reflection; no exploit/context proof — Vulnerability Candidate, not confirmed XSS.");
        }

        if (fp.Equals("asc.access.confirmed-unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            return new ClassificationProfile(
                FindingClass.Vulnerability,
                Severity.High,
                Exploitability.Demonstrated,
                DemonstratedImpact: true,
                RequiresManualValidation: false,
                BugBountyPolicyCategory.PrivilegeEscalation,
                "Verified unauthorized privileged access — Confirmed Vulnerability.");
        }

        if (fp.Equals("asc.access.surface-donotsubmit", StringComparison.OrdinalIgnoreCase))
        {
            return new ClassificationProfile(
                FindingClass.Informational,
                Severity.Informational,
                Exploitability.None,
                DemonstratedImpact: false,
                RequiresManualValidation: false,
                BugBountyPolicyCategory.ScannerOutputOnly,
                "Sensitive surface reachable but gated/public/harmless — Informational, DoNotSubmit.");
        }

        if (fp.Equals("asc.access.surface-manualreview-high", StringComparison.OrdinalIgnoreCase))
        {
            return new ClassificationProfile(
                FindingClass.VulnerabilityCandidate,
                Cap(scannerSeverity, Severity.High),
                Exploitability.RequiresPreconditions,
                DemonstratedImpact: false,
                RequiresManualValidation: true,
                BugBountyPolicyCategory.PrivilegeEscalation,
                "High-priority AccessControlCandidate: unauthenticated sensitive-data indicators.");
        }

        if (fp.StartsWith("asc.access.", StringComparison.OrdinalIgnoreCase))
        {
            return new ClassificationProfile(
                FindingClass.VulnerabilityCandidate,
                Cap(scannerSeverity, Severity.Medium),
                Exploitability.RequiresPreconditions,
                DemonstratedImpact: false,
                RequiresManualValidation: true,
                BugBountyPolicyCategory.PrivilegeEscalation,
                "AccessControlCandidate: path/surface observation only — not a vulnerability without privileged-access proof.");
        }

        if (fp.Equals("asc.cors.origin-reflection", StringComparison.OrdinalIgnoreCase))
        {
            return new ClassificationProfile(
                FindingClass.SecurityMisconfiguration,
                Cap(scannerSeverity, Severity.Medium),
                Exploitability.RequiresPreconditions,
                DemonstratedImpact: false,
                RequiresManualValidation: true,
                BugBountyPolicyCategory.MisconfigurationWithDemonstratedImpact,
                "CORS candidate; credentialed cross-origin impact requires Manual Review.");
        }

        if (fp.StartsWith("tls.", StringComparison.OrdinalIgnoreCase)
            || fp.StartsWith("https.", StringComparison.OrdinalIgnoreCase)
            || fp.Equals("http.mixed-content", StringComparison.OrdinalIgnoreCase)
            || checkCode.Contains("cors", StringComparison.OrdinalIgnoreCase))
        {
            return new ClassificationProfile(
                FindingClass.SecurityMisconfiguration,
                Cap(scannerSeverity, Severity.Medium),
                Exploitability.Theoretical,
                DemonstratedImpact: false,
                RequiresManualValidation: true,
                BugBountyPolicyCategory.MisconfigurationWithDemonstratedImpact,
                "Configuration finding; not Submit without demonstrated impact.");
        }

        return new ClassificationProfile(
            FindingClass.Informational,
            Cap(scannerSeverity, Severity.Low),
            Exploitability.None,
            DemonstratedImpact: false,
            RequiresManualValidation: true,
            BugBountyPolicyCategory.ScannerOutputOnly,
            "Passive scanner output only; real exploitability / impact not validated.");
    }

    private static Severity Cap(Severity scanner, Severity max) =>
        scanner > max ? max : scanner;

    private static string BuildReason(Finding finding, ClassificationProfile profile)
    {
        var eligible = finding.BugBountyEligible ? "Bug bounty eligible" : "Not bug bounty eligible";
        return $"{eligible}. Class={finding.FindingClass}, TechnicalSeverity={finding.TechnicalSeverity}, " +
               $"Exploitability={finding.Exploitability}, DemonstratedImpact={finding.DemonstratedImpact}, " +
               $"Recommendation={finding.SubmissionRecommendation}. {profile.ReasonDetail}";
    }

    private sealed record ClassificationProfile(
        FindingClass FindingClass,
        Severity TechnicalSeverity,
        Exploitability Exploitability,
        bool DemonstratedImpact,
        bool RequiresManualValidation,
        BugBountyPolicyCategory PolicyCategory,
        string ReasonDetail)
    {
        public string? EligibilityOverride { get; set; }
    }
}
