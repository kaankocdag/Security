using System.Net;
using System.Text.RegularExpressions;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Logging;
using ReflectionInputAnalyzer = Kaan.SecurityPlatform.Infrastructure.HackerOne.ReflectionInputAnalyzer;

namespace Kaan.SecurityPlatform.Infrastructure.HackerOne.Engines;

/// <summary>
/// Sensitive-surface probes via <see cref="ISensitiveSurfaceAnalyzer"/>.
/// Path existence (/admin, /dashboard, …) is NOT a vulnerability.
/// </summary>
public sealed class AccessControlCandidateEngine : IApplicationSecurityCandidateEngine
{
    private readonly SecureHttpClientFactory _httpFactory;
    private readonly ISensitiveSurfaceAnalyzer _surfaceAnalyzer;
    private readonly ILogger<AccessControlCandidateEngine> _logger;

    public AccessControlCandidateEngine(
        SecureHttpClientFactory httpFactory,
        ISensitiveSurfaceAnalyzer surfaceAnalyzer,
        ILogger<AccessControlCandidateEngine> logger)
    {
        _httpFactory = httpFactory;
        _surfaceAnalyzer = surfaceAnalyzer;
        _logger = logger;
    }

    public string EngineKey => "access-control";

    public async Task<IReadOnlyList<CandidateFindingDraft>> RunAsync(
        CandidateEngineContext context,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<CandidateFindingDraft>();
        // Manual redirect following inside SensitiveSurfaceAnalyzer — do not auto-redirect here.
        using var client = CreateClient(context, allowRedirects: false);

        foreach (var path in SensitiveSurfaceAnalyzer.DefaultSensitivePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var url = new Uri(context.BaseUri, path);
                var analysis = await _surfaceAnalyzer.InspectAsync(client, url, cancellationToken);

                // Skip pure not-found noise; existence alone never becomes Vulnerability.
                if (analysis.HttpStatusCode is 404 or 410 or 0)
                {
                    continue;
                }

                // Optional anonymous vs authorized compare may add ManualReviewReasons.
                if (!string.IsNullOrWhiteSpace(context.TestAccountUsername)
                    && !string.IsNullOrWhiteSpace(context.TestAccountPassword)
                    && !analysis.LoginPageDetected
                    && !analysis.AccessDeniedDetected
                    && analysis.HttpStatusCode is >= 200 and < 400)
                {
                    var comparison = await _surfaceAnalyzer.CompareAnonymousVsAuthorizedAsync(
                        client,
                        url,
                        context.TestAccountUsername!,
                        context.TestAccountPassword!,
                        cancellationToken);
                    if (comparison?.IndicatesSuspiciousPrivilegeExposure == true)
                    {
                        analysis = analysis.WithAdditionalManualReviewReasons(
                        [
                            "Anonymous vs authorized comparison indicates suspicious privilege exposure " +
                            "(status/redirect/fingerprint/feature delta with unauthenticated privileged or sensitive signals)."
                        ]);
                    }
                }

                findings.Add(ToDraft(analysis));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Sensitive surface probe failed for {Path}", path);
            }
        }

        return findings;
    }

    private static CandidateFindingDraft ToDraft(SensitiveSurfaceAnalysisResult analysis)
    {
        var isConfirmed = analysis.ConfirmedVulnerability && analysis.UnauthorizedPrivilegedAccess;
        var isManual = !isConfirmed
                       && analysis.SubmissionRecommendation == SubmissionRecommendation.ManualReview
                       && analysis.ManualReviewReasons.Count > 0;
        var title = isConfirmed
            ? $"Confirmed Broken Access Control — {GetPath(analysis.Url)}"
            : isManual && analysis.HighPriorityManualReview
                ? $"AccessControlCandidate [high priority] — {GetPath(analysis.Url)}"
                : isManual
                    ? $"AccessControlCandidate — sensitive surface {GetPath(analysis.Url)}"
                    : $"Sensitive surface observation — {GetPath(analysis.Url)}";

        return new CandidateFindingDraft(
            Title: title,
            Description: analysis.Reason,
            CheckCode: "asc.access-control",
            Fingerprint: analysis.Fingerprint,
            Severity: isConfirmed
                ? Severity.High
                : analysis.HighPriorityManualReview
                    ? Severity.Medium
                    : Severity.Informational,
            Category: "Access Control",
            AffectedUrl: analysis.Url,
            AffectedParameter: null,
            Evidence: Redact(analysis.FormatSurfaceEvidence()),
            Remediation: isConfirmed || isManual
                ? "Enforce authentication and authorization on privileged surfaces. Validate with dual-account tests before claiming Broken Access Control."
                : "No submission recommended. Path reachability without privileged-access proof is informational only.",
            CweCode: analysis.PotentialWeakness,
            OwaspCategory: "A01:2021-Broken Access Control");
    }

    private static string GetPath(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.AbsolutePath;
        }

        return url;
    }

    private HttpClient CreateClient(CandidateEngineContext context, bool allowRedirects)
    {
        var client = _httpFactory.Create(TimeSpan.FromSeconds(12), maxRedirects: 0, allowRedirects: allowRedirects);
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(context.UserAgent);
        return client;
    }

    private static string Redact(string value) =>
        value.Length <= 3500 ? value : value[..3500] + "…";
}

/// <summary>Harmless unique marker reflection — no executable JS / active exploitation.</summary>
public sealed class XssReflectionCandidateEngine : IApplicationSecurityCandidateEngine
{
    private readonly SecureHttpClientFactory _httpFactory;
    private readonly ILogger<XssReflectionCandidateEngine> _logger;

    public XssReflectionCandidateEngine(SecureHttpClientFactory httpFactory, ILogger<XssReflectionCandidateEngine> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public string EngineKey => "xss-reflection";

    public async Task<IReadOnlyList<CandidateFindingDraft>> RunAsync(
        CandidateEngineContext context,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<CandidateFindingDraft>();
        using var client = CreateClient(context);
        var marker = ReflectionInputAnalyzer.CreateHarmlessMarker();

        try
        {
            var builder = new UriBuilder(context.BaseUri) { Query = $"q={Uri.EscapeDataString(marker)}" };
            using var response = await client.GetAsync(builder.Uri, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var analysis = ReflectionInputAnalyzer.Analyze(
                marker,
                body,
                response.Headers,
                response.Content.Headers,
                (int)response.StatusCode);

            if (analysis.ReflectionCount <= 0)
            {
                return findings;
            }

            var meta = new CandidateReflectionMetadata(
                analysis.Context,
                analysis.ReflectionCount,
                analysis.HtmlEncoded,
                analysis.AttributeEncoded,
                analysis.ContentType,
                analysis.HttpStatus,
                analysis.ReflectionLocation,
                analysis.InputSource,
                analysis.Marker,
                analysis.ProperlyEncoded);

            findings.Add(new CandidateFindingDraft(
                Title: "Reflected input / XSS candidate (harmless marker)",
                Description:
                    "A unique harmless marker was reflected in the HTTP response. " +
                    "This is not XSS exploit proof; encoding/context Manual Review is required.",
                CheckCode: "asc.xss",
                Fingerprint: "asc.xss.reflected-marker",
                Severity: Severity.Medium,
                Category: "XSS",
                AffectedUrl: builder.Uri.ToString(),
                AffectedParameter: "q",
                Evidence: Redact(
                    $"ReflectionCount={analysis.ReflectionCount}; Context={analysis.Context}; " +
                    $"HtmlEncoded={analysis.HtmlEncoded}; AttributeEncoded={analysis.AttributeEncoded}; " +
                    $"Status={(int)response.StatusCode}"),
                Remediation: "Apply context-aware output encoding; confirm with browser PoC only after manual review.",
                CweCode: "CWE-79",
                OwaspCategory: "A03:2021-Injection",
                Reflection: meta));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "XSS reflection probe failed for {Host}", context.BaseUri.Host);
        }

        return findings;
    }

    private HttpClient CreateClient(CandidateEngineContext context)
    {
        var client = _httpFactory.Create(TimeSpan.FromSeconds(12));
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(context.UserAgent);
        return client;
    }

    private static string Redact(string value) =>
        value.Length <= 500 ? value : value[..500] + "…";
}

// Keep other engines from original file — Cors + InfoDisclosure
public sealed class CorsMisconfigCandidateEngine : IApplicationSecurityCandidateEngine
{
    private readonly SecureHttpClientFactory _httpFactory;
    private readonly ILogger<CorsMisconfigCandidateEngine> _logger;

    public CorsMisconfigCandidateEngine(SecureHttpClientFactory httpFactory, ILogger<CorsMisconfigCandidateEngine> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public string EngineKey => "cors";

    public async Task<IReadOnlyList<CandidateFindingDraft>> RunAsync(
        CandidateEngineContext context,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<CandidateFindingDraft>();
        using var client = CreateClient(context);
        const string evil = "https://evil.example";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, context.BaseUri);
            request.Headers.TryAddWithoutValidation("Origin", evil);
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values))
            {
                var acao = string.Join(",", values);
                if (acao.Contains(evil, StringComparison.OrdinalIgnoreCase) || acao.Trim() == "*")
                {
                    findings.Add(new CandidateFindingDraft(
                        Title: "CORS Origin reflection / wildcard candidate",
                        Description:
                            "Access-Control-Allow-Origin reflected a foreign Origin or used *. Credentialed impact requires Manual Review.",
                        CheckCode: "asc.cors",
                        Fingerprint: "asc.cors.origin-reflection",
                        Severity: Severity.Medium,
                        Category: "CORS",
                        AffectedUrl: context.BaseUri.ToString(),
                        AffectedParameter: "Origin",
                        Evidence: Redact($"Origin={evil}; ACAO={acao}"),
                        Remediation: "Reflect only trusted origins; avoid * with credentials.",
                        CweCode: "CWE-942",
                        OwaspCategory: "A05:2021-Security Misconfiguration"));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "CORS probe failed");
        }

        return findings;
    }

    private HttpClient CreateClient(CandidateEngineContext context)
    {
        var client = _httpFactory.Create(TimeSpan.FromSeconds(12));
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(context.UserAgent);
        return client;
    }

    private static string Redact(string value) =>
        value.Length <= 500 ? value : value[..500] + "…";
}

public sealed class InfoDisclosureCandidateEngine : IApplicationSecurityCandidateEngine
{
    private readonly SecureHttpClientFactory _httpFactory;
    private readonly ILogger<InfoDisclosureCandidateEngine> _logger;

    private static readonly Regex Sensitive = new(
        @"api[_-]?key\s*[:=]\s*['\""]?[A-Za-z0-9_\-]{12,}|password\s*[:=]\s*\S+|-----BEGIN (RSA )?PRIVATE KEY-----",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public InfoDisclosureCandidateEngine(SecureHttpClientFactory httpFactory, ILogger<InfoDisclosureCandidateEngine> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public string EngineKey => "info-disclosure";

    public async Task<IReadOnlyList<CandidateFindingDraft>> RunAsync(
        CandidateEngineContext context,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<CandidateFindingDraft>();
        using var client = CreateClient(context);
        string[] paths = ["/.env", "/.git/HEAD", "/server-status", "/phpinfo.php"];
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var url = new Uri(context.BaseUri, path);
                using var response = await client.GetAsync(url, cancellationToken);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    continue;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (body.Length < 20)
                {
                    continue;
                }

                var match = Sensitive.Match(body);
                var evidence = match.Success
                    ? $"[redacted sensitive pattern near index {match.Index}]"
                    : $"HTTP 200 on {path}; length={body.Length} (no high-confidence secret pattern).";

                findings.Add(new CandidateFindingDraft(
                    Title: $"Information disclosure candidate: {path}",
                    Description:
                        "A sensitive-looking path returned HTTP 200. Confirm real secret exposure manually before Submit.",
                    CheckCode: "asc.info",
                    Fingerprint: "asc.info.path-exposure",
                    Severity: match.Success ? Severity.Medium : Severity.Low,
                    Category: "Information Disclosure",
                    AffectedUrl: url.ToString(),
                    AffectedParameter: null,
                    Evidence: evidence,
                    Remediation: "Remove public exposure of debug/config endpoints; rotate any leaked credentials.",
                    CweCode: "CWE-200",
                    OwaspCategory: "A01:2021-Broken Access Control"));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Info disclosure probe failed for {Path}", path);
            }
        }

        return findings;
    }

    private HttpClient CreateClient(CandidateEngineContext context)
    {
        var client = _httpFactory.Create(TimeSpan.FromSeconds(12));
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(context.UserAgent);
        return client;
    }
}
