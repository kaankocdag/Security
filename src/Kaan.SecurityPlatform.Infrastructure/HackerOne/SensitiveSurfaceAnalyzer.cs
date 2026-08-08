using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Infrastructure.HackerOne;

/// <summary>
/// Safe, non-destructive analysis of sensitive-looking URL surfaces.
/// Path existence alone is never a vulnerability. ManualReview requires non-empty ManualReviewReasons.
/// </summary>
public interface ISensitiveSurfaceAnalyzer
{
    SensitiveSurfaceAnalysisResult Analyze(
        string url,
        int httpStatusCode,
        string finalUrl,
        IReadOnlyList<string> redirectChain,
        string? contentType,
        string? body,
        bool hasWwwAuthenticate = false,
        IEnumerable<string>? additionalManualReviewReasons = null);

    /// <summary>
    /// Only after human/authorized validation proves unauthorized privileged access.
    /// Passive path probes never call this automatically.
    /// </summary>
    SensitiveSurfaceAnalysisResult MarkVerifiedUnauthorizedPrivilegedAccess(
        SensitiveSurfaceAnalysisResult observation,
        string verificationEvidence);

    Task<SensitiveSurfaceAnalysisResult> InspectAsync(
        HttpClient client,
        Uri url,
        CancellationToken cancellationToken = default);

    Task<SensitiveSurfaceComparison?> CompareAnonymousVsAuthorizedAsync(
        HttpClient anonymousClient,
        Uri url,
        string testAccountUsername,
        string testAccountPassword,
        CancellationToken cancellationToken = default);
}

public sealed record SensitiveSurfaceAnalysisResult(
    string Url,
    int HttpStatusCode,
    string FinalUrl,
    IReadOnlyList<string> RedirectChain,
    string? ContentType,
    string? PageTitle,
    bool AuthenticationRequired,
    bool LoginPageDetected,
    bool AccessDeniedDetected,
    bool SensitiveContentDetected,
    bool PrivilegedFunctionalityDetected,
    bool SensitiveIdentifiersDetected,
    bool AuthenticationExpectedButMissing,
    string ResponseFingerprint,
    string AnalysisConfidence,
    bool UnauthorizedPrivilegedAccess,
    bool SensitiveDataExposure,
    SubmissionRecommendation SubmissionRecommendation,
    FindingClass FindingClass,
    string FindingType,
    string? PotentialWeakness,
    bool ConfirmedVulnerability,
    bool DemonstratedImpact,
    bool RequiresManualValidation,
    string EvidenceSummary,
    string Reason,
    IReadOnlyList<string> ManualReviewReasons,
    bool HighPriorityManualReview = false)
{
    /// <summary>
    /// Decision matrix fingerprints:
    /// DoNotSubmit | ManualReview | ManualReview (high) | Confirmed unauthorized access.
    /// </summary>
    public string Fingerprint
    {
        get
        {
            if (ConfirmedVulnerability && UnauthorizedPrivilegedAccess && DemonstratedImpact)
            {
                return "asc.access.confirmed-unauthorized";
            }

            if (SubmissionRecommendation == SubmissionRecommendation.ManualReview
                && HighPriorityManualReview)
            {
                return "asc.access.surface-manualreview-high";
            }

            if (SubmissionRecommendation == SubmissionRecommendation.ManualReview)
            {
                return "asc.access.surface-manualreview";
            }

            return "asc.access.surface-donotsubmit";
        }
    }

    public SensitiveSurfaceAnalysisResult WithAdditionalManualReviewReasons(IEnumerable<string> extra)
    {
        var merged = ManualReviewReasons
            .Concat(extra.Where(r => !string.IsNullOrWhiteSpace(r)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return SensitiveSurfaceAnalyzer.ClassifyFromSignals(
            Url,
            HttpStatusCode,
            FinalUrl,
            RedirectChain,
            ContentType,
            PageTitle,
            AuthenticationRequired,
            LoginPageDetected,
            AccessDeniedDetected,
            SensitiveContentDetected,
            PrivilegedFunctionalityDetected,
            SensitiveIdentifiersDetected,
            AuthenticationExpectedButMissing,
            ResponseFingerprint,
            AnalysisConfidence,
            UnauthorizedPrivilegedAccess,
            SensitiveDataExposure,
            EvidenceSummary,
            merged);
    }

    public string FormatSurfaceEvidence()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Surface Evidence");
        sb.AppendLine($"URL: {Url}");
        sb.AppendLine($"HTTP Status: {HttpStatusCode}");
        sb.AppendLine($"Final URL: {FinalUrl}");
        sb.AppendLine($"Redirect Chain: {(RedirectChain.Count == 0 ? "(none)" : string.Join(" → ", RedirectChain))}");
        sb.AppendLine($"Page Title: {PageTitle ?? "(none)"}");
        sb.AppendLine($"Content Type: {ContentType ?? "(none)"}");
        sb.AppendLine($"Authentication Required: {YesNo(AuthenticationRequired)}");
        sb.AppendLine($"Login Page Detected: {YesNo(LoginPageDetected)}");
        sb.AppendLine($"Access Denied Detected: {YesNo(AccessDeniedDetected)}");
        sb.AppendLine($"Sensitive Content Detected: {YesNo(SensitiveContentDetected)}");
        sb.AppendLine($"Privileged Functionality Detected: {YesNo(PrivilegedFunctionalityDetected)}");
        sb.AppendLine($"Sensitive Identifiers Detected: {YesNo(SensitiveIdentifiersDetected)}");
        sb.AppendLine($"Authentication Expected But Missing: {YesNo(AuthenticationExpectedButMissing)}");
        sb.AppendLine($"Unauthorized Privileged Access: {YesNo(UnauthorizedPrivilegedAccess)}");
        sb.AppendLine($"Sensitive Data Exposure: {YesNo(SensitiveDataExposure)}");
        sb.AppendLine($"Confirmed Vulnerability: {YesNo(ConfirmedVulnerability)}");
        sb.AppendLine($"High Priority Manual Review: {YesNo(HighPriorityManualReview)}");
        sb.AppendLine($"Analysis Confidence: {AnalysisConfidence}");
        sb.AppendLine($"Response Fingerprint: {ResponseFingerprint}");
        sb.AppendLine($"Evidence Summary: {EvidenceSummary}");
        if (ManualReviewReasons.Count > 0)
        {
            sb.AppendLine("ManualReviewReasons:");
            foreach (var reason in ManualReviewReasons)
            {
                sb.AppendLine($"- {reason}");
            }
        }
        else
        {
            sb.AppendLine("ManualReviewReasons: (none)");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Builds HackerOne Steps to Reproduce from recorded HTTP observations only.
    /// Never emits generic placeholder text like "Reproduce the described candidate behavior."
    /// </summary>
    public string FormatStepsFromObservations()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"1. Send a safe GET request to `{Url}` (no authentication bypass, no credential stuffing, no privilege escalation).");
        sb.AppendLine($"2. Observe the HTTP response: status `{HttpStatusCode}`, final URL `{FinalUrl}`.");
        if (RedirectChain.Count > 0)
        {
            sb.AppendLine($"3. Recorded redirect chain: {string.Join(" → ", RedirectChain)}.");
            sb.AppendLine(
                $"4. Recorded surface signals: LoginPageDetected={YesNo(LoginPageDetected)}; " +
                $"AccessDeniedDetected={YesNo(AccessDeniedDetected)}; " +
                $"PrivilegedFunctionalityDetected={YesNo(PrivilegedFunctionalityDetected)}; " +
                $"SensitiveContentDetected={YesNo(SensitiveContentDetected)}.");
            AppendManualReviewStep(sb, 5);
        }
        else
        {
            sb.AppendLine(
                $"3. Recorded surface signals: LoginPageDetected={YesNo(LoginPageDetected)}; " +
                $"AccessDeniedDetected={YesNo(AccessDeniedDetected)}; " +
                $"PrivilegedFunctionalityDetected={YesNo(PrivilegedFunctionalityDetected)}; " +
                $"SensitiveContentDetected={YesNo(SensitiveContentDetected)}.");
            AppendManualReviewStep(sb, 4);
        }

        return sb.ToString().TrimEnd();
    }

    private void AppendManualReviewStep(StringBuilder sb, int stepNumber)
    {
        if (ManualReviewReasons.Count > 0)
        {
            sb.AppendLine($"{stepNumber}. ManualReviewReasons observed:");
            foreach (var reason in ManualReviewReasons)
            {
                sb.AppendLine($"   - {reason}");
            }

            sb.AppendLine(
                $"{stepNumber + 1}. Path existence alone is not a vulnerability; do not claim Broken Access Control without privilege-boundary proof.");
        }
        else
        {
            sb.AppendLine(
                $"{stepNumber}. No ManualReviewReasons were recorded — classification is Informational / DoNotSubmit " +
                $"(ConfirmedVulnerability={YesNo(ConfirmedVulnerability)}; SubmissionRecommendation={SubmissionRecommendation}).");
        }
    }

    private static string YesNo(bool value) => value ? "Yes" : "No";
}

public sealed record SensitiveSurfaceComparison(
    SensitiveSurfaceAnalysisResult Anonymous,
    SensitiveSurfaceAnalysisResult Authorized,
    bool StatusCodesDiffer,
    bool RedirectsDiffer,
    bool FingerprintsDiffer,
    bool VisibleFeatureDifferences,
    bool IndicatesSuspiciousPrivilegeExposure,
    string Summary);

public sealed class SensitiveSurfaceAnalyzer : ISensitiveSurfaceAnalyzer
{
    public static readonly string[] DefaultSensitivePaths =
    [
        "/admin",
        "/administrator",
        "/dashboard",
        "/manage",
        "/management",
        "/internal",
        "/backend",
        "/console",
        "/account",
        "/settings",
        "/api/me"
    ];

    private static readonly Regex TitleRegex = new(
        @"<title[^>]*>(.*?)</title>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex EmailRegex = new(
        @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled);

    private static readonly Regex UuidRegex = new(
        @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
        RegexOptions.Compiled);

    private const int MaxRedirects = 5;
    private const int MaxBodyChars = 256_000;

    public SensitiveSurfaceAnalysisResult Analyze(
        string url,
        int httpStatusCode,
        string finalUrl,
        IReadOnlyList<string> redirectChain,
        string? contentType,
        string? body,
        bool hasWwwAuthenticate = false,
        IEnumerable<string>? additionalManualReviewReasons = null)
    {
        body ??= string.Empty;
        if (body.Length > MaxBodyChars)
        {
            body = body[..MaxBodyChars];
        }

        var lower = body.ToLowerInvariant();
        var title = ExtractTitle(body);
        var login = DetectLoginPage(lower, title) || IsAuthRedirect(finalUrl, redirectChain);
        var accessDenied = DetectAccessDenied(httpStatusCode, lower, title);
        var gated = login || accessDenied || httpStatusCode is 401 or 403;

        var privileged = !gated && DetectPrivilegedFunctionality(lower, title);
        var identifiers = !gated && DetectSensitiveIdentifiers(body, lower);
        var sensitiveContent = !gated && (DetectSensitiveContent(lower, title) || identifiers);
        var authRequired = hasWwwAuthenticate || httpStatusCode is 401 or 403 || login || accessDenied;
        var authExpectedMissing = !gated
                                  && LooksLikeSensitivePath(url)
                                  && httpStatusCode is >= 200 and < 300
                                  && !hasWwwAuthenticate
                                  && (privileged || sensitiveContent || identifiers || LooksLikeAuthenticatedAppShell(lower));

        var fingerprint = ComputeFingerprint(httpStatusCode, finalUrl, contentType, title, body);
        var confidence = ComputeConfidence(login, accessDenied, privileged, sensitiveContent, identifiers, httpStatusCode);

        var reasons = new List<string>();
        if (!gated)
        {
            if (privileged)
            {
                reasons.Add("Unauthenticated response contains administrative action controls.");
            }

            // Unauthenticated sensitive data = high-priority ManualReview.
            if (identifiers)
            {
                reasons.Add(
                    "[high priority] Sensitive account identifiers detected in unauthenticated response.");
            }
            else if (sensitiveContent)
            {
                reasons.Add(
                    "[high priority] Unauthenticated response contains sensitive-data indicators.");
            }

            if (authExpectedMissing && reasons.Count == 0)
            {
                reasons.Add(
                    "Authentication appears expected on this sensitive path but was not enforced (authenticated app-shell indicators without login/access-denied).");
            }
            else if (authExpectedMissing && (privileged || sensitiveContent || identifiers))
            {
                reasons.Add("Authentication expected on sensitive path appears missing (no login/access-denied gate).");
            }
        }

        if (additionalManualReviewReasons is not null)
        {
            reasons.AddRange(additionalManualReviewReasons.Where(r => !string.IsNullOrWhiteSpace(r)));
        }

        reasons = reasons.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        return ClassifyFromSignals(
            url,
            httpStatusCode,
            finalUrl,
            redirectChain,
            contentType,
            title,
            authRequired,
            login,
            accessDenied,
            sensitiveContent,
            privileged,
            identifiers,
            authExpectedMissing,
            fingerprint,
            confidence,
            unauthorizedPrivilegedAccess: false,
            sensitiveDataExposure: sensitiveContent || identifiers,
            evidenceSummary: reasons.Count > 0
                ? string.Join(" ", reasons)
                : BuildSafeEvidenceSummary(login, accessDenied, httpStatusCode),
            reasons);
    }

    public SensitiveSurfaceAnalysisResult MarkVerifiedUnauthorizedPrivilegedAccess(
        SensitiveSurfaceAnalysisResult observation,
        string verificationEvidence)
    {
        if (string.IsNullOrWhiteSpace(verificationEvidence))
        {
            throw new ArgumentException(
                "Verification evidence is required to confirm unauthorized privileged access.",
                nameof(verificationEvidence));
        }

        var reasons = observation.ManualReviewReasons
            .Append($"Verified unauthorized privileged access: {verificationEvidence.Trim()}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SensitiveSurfaceAnalysisResult(
            Url: observation.Url,
            HttpStatusCode: observation.HttpStatusCode,
            FinalUrl: observation.FinalUrl,
            RedirectChain: observation.RedirectChain,
            ContentType: observation.ContentType,
            PageTitle: observation.PageTitle,
            AuthenticationRequired: observation.AuthenticationRequired,
            LoginPageDetected: false,
            AccessDeniedDetected: false,
            SensitiveContentDetected: observation.SensitiveContentDetected,
            PrivilegedFunctionalityDetected: true,
            SensitiveIdentifiersDetected: observation.SensitiveIdentifiersDetected,
            AuthenticationExpectedButMissing: observation.AuthenticationExpectedButMissing,
            ResponseFingerprint: observation.ResponseFingerprint,
            AnalysisConfidence: "High",
            UnauthorizedPrivilegedAccess: true,
            SensitiveDataExposure: observation.SensitiveDataExposure,
            SubmissionRecommendation: SubmissionRecommendation.Submit,
            FindingClass: FindingClass.Vulnerability,
            FindingType: "Broken Access Control",
            PotentialWeakness: "CWE-284",
            ConfirmedVulnerability: true,
            DemonstratedImpact: true,
            RequiresManualValidation: false,
            EvidenceSummary: verificationEvidence.Trim(),
            Reason:
                "Verified unauthorized privileged access was demonstrated with authorized validation evidence. " +
                "Confirmed Vulnerability: Yes.",
            ManualReviewReasons: reasons,
            HighPriorityManualReview: false);
    }

    internal static SensitiveSurfaceAnalysisResult ClassifyFromSignals(
        string url,
        int httpStatusCode,
        string finalUrl,
        IReadOnlyList<string> redirectChain,
        string? contentType,
        string? pageTitle,
        bool authenticationRequired,
        bool loginPageDetected,
        bool accessDeniedDetected,
        bool sensitiveContentDetected,
        bool privilegedFunctionalityDetected,
        bool sensitiveIdentifiersDetected,
        bool authenticationExpectedButMissing,
        string responseFingerprint,
        string analysisConfidence,
        bool unauthorizedPrivilegedAccess,
        bool sensitiveDataExposure,
        string evidenceSummary,
        IReadOnlyList<string> manualReviewReasons)
    {
        // Hard rule: empty reasons => DoNotSubmit. Path name alone never ManualReview.
        if (manualReviewReasons.Count == 0
            || loginPageDetected
            || accessDeniedDetected
            || httpStatusCode is 401 or 403)
        {
            return new SensitiveSurfaceAnalysisResult(
                Url: url,
                HttpStatusCode: httpStatusCode,
                FinalUrl: finalUrl,
                RedirectChain: redirectChain,
                ContentType: contentType,
                PageTitle: pageTitle,
                AuthenticationRequired: authenticationRequired,
                LoginPageDetected: loginPageDetected,
                AccessDeniedDetected: accessDeniedDetected || httpStatusCode is 401 or 403,
                SensitiveContentDetected: false,
                PrivilegedFunctionalityDetected: false,
                SensitiveIdentifiersDetected: false,
                AuthenticationExpectedButMissing: false,
                ResponseFingerprint: responseFingerprint,
                AnalysisConfidence: analysisConfidence,
                UnauthorizedPrivilegedAccess: false,
                SensitiveDataExposure: false,
                SubmissionRecommendation: SubmissionRecommendation.DoNotSubmit,
                FindingClass: FindingClass.Informational,
                FindingType: "Informational",
                PotentialWeakness: null,
                ConfirmedVulnerability: false,
                DemonstratedImpact: false,
                RequiresManualValidation: false,
                EvidenceSummary: evidenceSummary,
                Reason:
                    "Sensitive-looking path is reachable, but no unauthorized privileged access " +
                    "or sensitive data exposure was demonstrated.",
                ManualReviewReasons: Array.Empty<string>(),
                HighPriorityManualReview: false);
        }

        var highPriority = sensitiveContentDetected || sensitiveIdentifiersDetected
                           || manualReviewReasons.Any(r =>
                               r.Contains("[high priority]", StringComparison.OrdinalIgnoreCase));

        return new SensitiveSurfaceAnalysisResult(
            Url: url,
            HttpStatusCode: httpStatusCode,
            FinalUrl: finalUrl,
            RedirectChain: redirectChain,
            ContentType: contentType,
            PageTitle: pageTitle,
            AuthenticationRequired: authenticationRequired,
            LoginPageDetected: loginPageDetected,
            AccessDeniedDetected: accessDeniedDetected,
            SensitiveContentDetected: sensitiveContentDetected,
            PrivilegedFunctionalityDetected: privilegedFunctionalityDetected,
            SensitiveIdentifiersDetected: sensitiveIdentifiersDetected,
            AuthenticationExpectedButMissing: authenticationExpectedButMissing,
            ResponseFingerprint: responseFingerprint,
            AnalysisConfidence: highPriority ? "High" : analysisConfidence,
            UnauthorizedPrivilegedAccess: unauthorizedPrivilegedAccess,
            SensitiveDataExposure: sensitiveDataExposure,
            SubmissionRecommendation: SubmissionRecommendation.ManualReview,
            FindingClass: FindingClass.VulnerabilityCandidate,
            FindingType: "AccessControlCandidate",
            PotentialWeakness: "CWE-284",
            ConfirmedVulnerability: false,
            DemonstratedImpact: false,
            RequiresManualValidation: true,
            EvidenceSummary: evidenceSummary,
            Reason: highPriority
                ? "High-priority AccessControlCandidate: unauthenticated sensitive-data indicators were observed. " +
                  "ConfirmedVulnerability=false until unauthorized privileged access is verified."
                : "Meaningful unauthenticated security signals were observed on a sensitive-looking path. " +
                  "ConfirmedVulnerability=false — Manual Review required. Path existence alone is not a vulnerability.",
            ManualReviewReasons: manualReviewReasons,
            HighPriorityManualReview: highPriority);
    }

    public async Task<SensitiveSurfaceAnalysisResult> InspectAsync(
        HttpClient client,
        Uri url,
        CancellationToken cancellationToken = default)
    {
        var (status, finalUrl, chain, contentType, body, wwwAuth) =
            await FetchWithRedirectChainAsync(client, url, authHeader: null, cancellationToken);
        return Analyze(url.ToString(), status, finalUrl, chain, contentType, body, wwwAuth);
    }

    public async Task<SensitiveSurfaceComparison?> CompareAnonymousVsAuthorizedAsync(
        HttpClient anonymousClient,
        Uri url,
        string testAccountUsername,
        string testAccountPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(testAccountUsername) || string.IsNullOrWhiteSpace(testAccountPassword))
        {
            return null;
        }

        var anonymous = await InspectAsync(anonymousClient, url, cancellationToken);
        var basic = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{testAccountUsername}:{testAccountPassword}"));
        var authHeader = new AuthenticationHeaderValue("Basic", basic);

        var (status, finalUrl, chain, contentType, body, wwwAuth) =
            await FetchWithRedirectChainAsync(anonymousClient, url, authHeader, cancellationToken);
        var authorized = Analyze(url.ToString(), status, finalUrl, chain, contentType, body, wwwAuth);

        var statusDiff = anonymous.HttpStatusCode != authorized.HttpStatusCode;
        var redirectDiff = !anonymous.RedirectChain.SequenceEqual(authorized.RedirectChain, StringComparer.OrdinalIgnoreCase)
                           || !string.Equals(anonymous.FinalUrl, authorized.FinalUrl, StringComparison.OrdinalIgnoreCase);
        var fpDiff = !string.Equals(anonymous.ResponseFingerprint, authorized.ResponseFingerprint, StringComparison.Ordinal);
        var featureDiff =
            anonymous.LoginPageDetected != authorized.LoginPageDetected
            || anonymous.PrivilegedFunctionalityDetected != authorized.PrivilegedFunctionalityDetected
            || anonymous.AccessDeniedDetected != authorized.AccessDeniedDetected
            || anonymous.SensitiveContentDetected != authorized.SensitiveContentDetected
            || anonymous.SensitiveIdentifiersDetected != authorized.SensitiveIdentifiersDetected;

        var anonymousExposed =
            anonymous.PrivilegedFunctionalityDetected
            || anonymous.SensitiveContentDetected
            || anonymous.SensitiveIdentifiersDetected;

        var suspicious = anonymousExposed
                         && !anonymous.LoginPageDetected
                         && !anonymous.AccessDeniedDetected
                         && (statusDiff || redirectDiff || fpDiff || featureDiff);

        var summary =
            $"Anonymous vs authorized (platform test account) comparison for `{url}` — " +
            $"statusDiffer={statusDiff}; redirectsDiffer={redirectDiff}; fingerprintsDiffer={fpDiff}; " +
            $"visibleFeatureDifferences={featureDiff}; suspiciousPrivilegeExposure={suspicious}. " +
            "Comparison records status/redirect/fingerprint/feature deltas only; does not claim authorization bypass.";

        return new SensitiveSurfaceComparison(
            anonymous, authorized, statusDiff, redirectDiff, fpDiff, featureDiff, suspicious, summary);
    }

    private static async Task<(
        int Status,
        string FinalUrl,
        IReadOnlyList<string> Chain,
        string? ContentType,
        string Body,
        bool WwwAuthenticate)> FetchWithRedirectChainAsync(
        HttpClient client,
        Uri startUrl,
        AuthenticationHeaderValue? authHeader,
        CancellationToken cancellationToken)
    {
        var chain = new List<string>();
        var current = startUrl;
        string? contentType = null;
        var body = string.Empty;
        var status = 0;
        var wwwAuth = false;
        var finalUrl = startUrl.ToString();

        for (var hop = 0; hop <= MaxRedirects; hop++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, current);
            if (authHeader is not null)
            {
                request.Headers.Authorization = authHeader;
            }

            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            status = (int)response.StatusCode;
            finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? current.ToString();
            contentType = response.Content.Headers.ContentType?.ToString();
            wwwAuth = response.Headers.WwwAuthenticate.Count > 0
                      || response.Headers.Contains("WWW-Authenticate");

            if (IsRedirect(response.StatusCode))
            {
                var location = response.Headers.Location;
                if (location is null)
                {
                    body = await ReadBodySafeAsync(response, cancellationToken);
                    break;
                }

                var next = location.IsAbsoluteUri ? location : new Uri(current, location);
                chain.Add($"{current} → {(int)response.StatusCode} → {next}");
                current = next;
                continue;
            }

            body = await ReadBodySafeAsync(response, cancellationToken);
            break;
        }

        return (status, finalUrl, chain, contentType, body, wwwAuth);
    }

    private static async Task<string> ReadBodySafeAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            return text.Length > MaxBodyChars ? text[..MaxBodyChars] : text;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static bool IsRedirect(HttpStatusCode code) =>
        code is HttpStatusCode.Moved
            or HttpStatusCode.Redirect
            or HttpStatusCode.RedirectMethod
            or HttpStatusCode.RedirectKeepVerb
            or HttpStatusCode.PermanentRedirect
            or (HttpStatusCode)308;

    private static bool IsAuthRedirect(string finalUrl, IReadOnlyList<string> redirectChain)
    {
        if (LooksLikeAuthUrl(finalUrl))
        {
            return true;
        }

        return redirectChain.Any(LooksLikeAuthUrl);
    }

    private static bool LooksLikeAuthUrl(string value)
    {
        var lower = value.ToLowerInvariant();
        return lower.Contains("/login")
               || lower.Contains("/signin")
               || lower.Contains("/sign-in")
               || lower.Contains("/auth/")
               || lower.Contains("oauth")
               || lower.Contains("/sso")
               || lower.Contains("/account/login");
    }

    private static bool LooksLikeSensitivePath(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && !Uri.TryCreate(url, UriKind.Relative, out uri))
        {
            return DefaultSensitivePaths.Any(p => url.Contains(p, StringComparison.OrdinalIgnoreCase));
        }

        var path = uri.IsAbsoluteUri ? uri.AbsolutePath : url;
        return DefaultSensitivePaths.Any(p =>
            path.Equals(p, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static bool LooksLikeAuthenticatedAppShell(string lower) =>
        lower.Contains("sign out")
        || lower.Contains("log out")
        || lower.Contains("signed in as")
        || lower.Contains("logged in as")
        || (lower.Contains("logout") && (lower.Contains("csrf") || lower.Contains("session")));

    private static string? ExtractTitle(string body)
    {
        var m = TitleRegex.Match(body);
        if (!m.Success)
        {
            return null;
        }

        var title = WebUtility.HtmlDecode(m.Groups[1].Value).Trim();
        title = Regex.Replace(title, @"\s+", " ");
        return title.Length > 200 ? title[..200] : title;
    }

    private static bool DetectLoginPage(string lower, string? title)
    {
        if (lower.Contains("type=\"password\"") || lower.Contains("type='password'") || lower.Contains("name=\"password\""))
        {
            return true;
        }

        var titleLower = title?.ToLowerInvariant() ?? string.Empty;
        if (titleLower.Contains("login") || titleLower.Contains("sign in") || titleLower.Contains("log in"))
        {
            return true;
        }

        var loginSignals = 0;
        if (lower.Contains("sign in") || lower.Contains("log in") || lower.Contains(">login<") || lower.Contains("/login"))
        {
            loginSignals++;
        }

        if (lower.Contains("password") && (lower.Contains("<form") || lower.Contains("username") || lower.Contains("email")))
        {
            loginSignals++;
        }

        if (lower.Contains("forgot password") || lower.Contains("remember me") || lower.Contains("oauth"))
        {
            loginSignals++;
        }

        return loginSignals >= 2;
    }

    private static bool DetectAccessDenied(int status, string lower, string? title)
    {
        if (status is 401 or 403)
        {
            return true;
        }

        var titleLower = title?.ToLowerInvariant() ?? string.Empty;
        if (titleLower.Contains("access denied")
            || titleLower.Contains("forbidden")
            || titleLower.Contains("unauthorized"))
        {
            return true;
        }

        return lower.Contains("access denied")
               || lower.Contains("you are not authorized")
               || lower.Contains("not authorised")
               || lower.Contains("permission denied")
               || (lower.Contains("forbidden") && lower.Contains("403"));
    }

    private static bool DetectPrivilegedFunctionality(string lower, string? title)
    {
        var titleLower = title?.ToLowerInvariant() ?? string.Empty;
        string[] strong =
        [
            "user management",
            "manage users",
            "delete user",
            "role assignment",
            "admin dashboard",
            "administration panel",
            "privileged",
            "superuser",
            "sudo",
            "impersonate",
            "billing admin",
            "tenant admin",
            "system settings",
            "internal console",
            "moderation queue"
        ];

        if (strong.Any(s => lower.Contains(s) || titleLower.Contains(s)))
        {
            return true;
        }

        var weakHits = 0;
        string[] weak =
        [
            "admin panel",
            "control panel",
            "manage roles",
            "permissions",
            "audit log",
            "create user",
            "ban user",
            "api keys",
            "webhook secret"
        ];

        foreach (var w in weak)
        {
            if (lower.Contains(w) || titleLower.Contains(w))
            {
                weakHits++;
            }
        }

        // "dashboard" alone is never enough — path name / weak word must not trigger ManualReview.
        return weakHits >= 2;
    }

    private static bool DetectSensitiveContent(string lower, string? title)
    {
        var titleLower = title?.ToLowerInvariant() ?? string.Empty;
        string[] markers =
        [
            "confidential",
            "internal only",
            "do not distribute",
            "customer pii",
            "social security",
            "credit card",
            "account balance",
            "salary",
            "private key",
            "-----begin",
            "aws_secret",
            "api_key",
            "client_secret"
        ];

        return markers.Any(m => lower.Contains(m) || titleLower.Contains(m));
    }

    private static bool DetectSensitiveIdentifiers(string body, string lower)
    {
        if (EmailRegex.Matches(body).Count >= 3)
        {
            return true;
        }

        if (UuidRegex.Matches(body).Count >= 5 && (lower.Contains("user") || lower.Contains("account") || lower.Contains("admin")))
        {
            return true;
        }

        return lower.Contains("ssn") || lower.Contains("passport number");
    }

    private static string ComputeFingerprint(
        int status,
        string finalUrl,
        string? contentType,
        string? title,
        string body)
    {
        var sample = body.Length <= 2048 ? body : body[..2048];
        sample = Regex.Replace(sample, @"\s+", " ").Trim().ToLowerInvariant();
        var material = $"{status}|{finalUrl}|{contentType}|{title}|{sample.Length}|{sample}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
    }

    private static string ComputeConfidence(
        bool login,
        bool denied,
        bool privileged,
        bool sensitive,
        bool identifiers,
        int status)
    {
        if ((login || denied) && !privileged && !sensitive)
        {
            return "High";
        }

        if (privileged && (sensitive || identifiers))
        {
            return "Medium";
        }

        if (privileged || sensitive)
        {
            return "Medium";
        }

        if (status is >= 200 and < 400)
        {
            return "Medium";
        }

        return "Low";
    }

    private static string BuildSafeEvidenceSummary(bool login, bool denied, int status) =>
        (login, denied, status) switch
        {
            (true, _, _) => "Reachable sensitive-looking path resolves to a login / authentication surface.",
            (_, true, _) => "Reachable sensitive-looking path returns access denied / unauthorized.",
            (_, _, >= 200 and < 300) => "Reachable sensitive-looking path serves a public/harmless surface without privileged indicators.",
            _ => "Sensitive-looking path probed; no unauthorized privileged access or sensitive data exposure demonstrated."
        };

    /// <summary>
    /// Rebuilds Steps to Reproduce from a persisted Surface Evidence block (finding.Evidence).
    /// Falls back to a safe observation-based template when fields are missing.
    /// </summary>
    public static string BuildStepsFromSurfaceEvidence(string? evidence, string safeTargetUrl)
    {
        var url = ReadEvidenceField(evidence, "URL:") ?? safeTargetUrl;
        var status = ReadEvidenceField(evidence, "HTTP Status:") ?? "(unknown)";
        var finalUrl = ReadEvidenceField(evidence, "Final URL:") ?? url;
        var redirects = ReadEvidenceField(evidence, "Redirect Chain:");
        var login = ReadEvidenceField(evidence, "Login Page Detected:") ?? "No";
        var denied = ReadEvidenceField(evidence, "Access Denied Detected:") ?? "No";
        var privileged = ReadEvidenceField(evidence, "Privileged Functionality Detected:") ?? "No";
        var sensitive = ReadEvidenceField(evidence, "Sensitive Content Detected:") ?? "No";
        var reasons = ExtractManualReviewReasonsFromEvidence(evidence);

        var sb = new StringBuilder();
        sb.AppendLine($"1. Target: {safeTargetUrl}");
        sb.AppendLine($"2. Send a safe GET to `{url}` (no auth stuffing / bypass / privilege escalation).");
        sb.AppendLine($"3. Observed HTTP status `{status}`; final URL `{finalUrl}`.");
        var step = 4;
        if (!string.IsNullOrWhiteSpace(redirects) && !redirects.Equals("(none)", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine($"{step}. Recorded redirect chain: {redirects}");
            step++;
        }

        sb.AppendLine(
            $"{step}. Recorded surface signals: LoginPageDetected={login}; AccessDeniedDetected={denied}; " +
            $"PrivilegedFunctionalityDetected={privileged}; SensitiveContentDetected={sensitive}.");
        step++;

        if (reasons.Count > 0)
        {
            sb.AppendLine($"{step}. ManualReviewReasons observed:");
            foreach (var reason in reasons)
            {
                sb.AppendLine($"   - {reason}");
            }

            step++;
            sb.AppendLine(
                $"{step}. Path existence alone is not a vulnerability; validate privilege-boundary impact before Submit.");
        }
        else
        {
            sb.AppendLine(
                $"{step}. No ManualReviewReasons were recorded — treat as Informational / DoNotSubmit unless further authorized validation demonstrates unauthorized privileged access.");
        }

        return sb.ToString().TrimEnd();
    }

    private static string? ReadEvidenceField(string? evidence, string label)
    {
        if (string.IsNullOrWhiteSpace(evidence))
        {
            return null;
        }

        foreach (var raw in evidence.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith(label, StringComparison.OrdinalIgnoreCase))
            {
                return line[label.Length..].Trim();
            }
        }

        return null;
    }

    private static IReadOnlyList<string> ExtractManualReviewReasonsFromEvidence(string? evidence)
    {
        if (string.IsNullOrWhiteSpace(evidence))
        {
            return Array.Empty<string>();
        }

        var reasons = new List<string>();
        var inBlock = false;
        foreach (var raw in evidence.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("ManualReviewReasons:", StringComparison.OrdinalIgnoreCase))
            {
                inBlock = true;
                if (line.Contains("(none)", StringComparison.OrdinalIgnoreCase))
                {
                    return Array.Empty<string>();
                }

                continue;
            }

            if (!inBlock)
            {
                continue;
            }

            if (line.StartsWith('-'))
            {
                var reason = line.TrimStart('-', ' ').Trim();
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    reasons.Add(reason);
                }

                continue;
            }

            if (line.Length == 0 || line.Contains(':', StringComparison.Ordinal))
            {
                break;
            }
        }

        return reasons;
    }
}
