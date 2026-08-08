using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using DnsClient;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.HackerOne.Engines;

/// <summary>
/// Pasif subdomain takeover sinyali: hedefin CNAME'i, içerik barındırmayan
/// (dangling) bir bulut sağlayıcısına işaret ediyorsa ve o sağlayıcı
/// "claim edilmemiş" parmak izini döndürüyorsa aday üretir. Saldırı yok,
/// yalnızca DNS + tek GET ile tespit.
/// </summary>
public sealed class SubdomainTakeoverCandidateEngine(
    SecureHttpClientFactory httpFactory,
    ILogger<SubdomainTakeoverCandidateEngine> logger) : IApplicationSecurityCandidateEngine
{
    private sealed record Provider(string Service, string[] CnameSuffixes, string[] Fingerprints);

    private static readonly Provider[] Providers =
    [
        new("GitHub Pages", ["github.io"], ["There isn't a GitHub Pages site here", "For root URLs (like http://example.com/) you must provide an index.html file"]),
        new("Heroku", ["herokudns.com", "herokuapp.com", "herokussl.com"], ["No such app", "herokucdn.com/error-pages/no-such-app.html"]),
        new("AWS S3", ["s3.amazonaws.com", "s3-website"], ["NoSuchBucket", "The specified bucket does not exist"]),
        new("Azure", ["azurewebsites.net", "cloudapp.net", "trafficmanager.net", "blob.core.windows.net"], ["404 Web Site not found", "The specified blob does not exist"]),
        new("Fastly", ["fastly.net"], ["Fastly error: unknown domain"]),
        new("Shopify", ["myshopify.com"], ["Sorry, this shop is currently unavailable", "Only one step left!"]),
        new("Zendesk", ["zendesk.com"], ["Help Center Closed"]),
        new("Unbounce", ["unbouncepages.com"], ["The requested URL was not found on this server"]),
        new("Surge.sh", ["surge.sh"], ["project not found"]),
        new("Pantheon", ["pantheonsite.io"], ["The gods are wise, but do not know of the site which you seek"])
    ];

    public string EngineKey => "subdomain-takeover";

    public async Task<IReadOnlyList<CandidateFindingDraft>> RunAsync(
        CandidateEngineContext context,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<CandidateFindingDraft>();
        var host = context.TargetHost;

        LookupClient dns;
        try
        {
            dns = new LookupClient();
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "DNS resolver init failed");
            return findings;
        }

        string? cname = null;
        try
        {
            var result = await dns.QueryAsync(host, QueryType.CNAME, cancellationToken: cancellationToken);
            cname = result.Answers.CnameRecords().FirstOrDefault()?.CanonicalName.Value.TrimEnd('.');
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "CNAME lookup failed for {Host}", host);
        }

        if (string.IsNullOrWhiteSpace(cname))
        {
            return findings;
        }

        var provider = Providers.FirstOrDefault(p =>
            p.CnameSuffixes.Any(s => cname.Contains(s, StringComparison.OrdinalIgnoreCase)));
        if (provider is null)
        {
            return findings;
        }

        // Sağlayıcı bilinen bir "claim edilmemiş" parmak izi döndürüyor mu?
        var fingerprintHit = false;
        try
        {
            using var client = CreateClient(context);
            using var response = await client.GetAsync(context.BaseUri, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            fingerprintHit = provider.Fingerprints.Any(f => body.Contains(f, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Takeover fingerprint fetch failed for {Host}", host);
        }

        var severity = fingerprintHit ? Severity.High : Severity.Low;
        findings.Add(new CandidateFindingDraft(
            Title: fingerprintHit
                ? $"Subdomain takeover candidate — dangling {provider.Service} ({host})"
                : $"Dangling CNAME to {provider.Service} — verify ({host})",
            Description: fingerprintHit
                ? $"{host} CNAME → {cname} ({provider.Service}) ve sağlayıcı claim edilmemiş kaynak parmak izi döndürdü. Takeover riski. Manuel doğrulama sonrası raporlanmalı."
                : $"{host} CNAME → {cname} ({provider.Service}). Kaynak aktif görünüyor; yine de sahiplik/askıda kalma manuel doğrulanmalı.",
            CheckCode: "asc.takeover",
            Fingerprint: $"asc.takeover.{provider.Service.Replace(' ', '-').ToLowerInvariant()}",
            Severity: severity,
            Category: "Subdomain Takeover",
            AffectedUrl: context.BaseUri.ToString(),
            AffectedParameter: null,
            Evidence: $"CNAME={cname}; Provider={provider.Service}; FingerprintMatched={fingerprintHit}",
            Remediation: "Kullanılmayan DNS kayıtlarını kaldırın veya kaynağı yeniden talep edin (reclaim). Askıda CNAME bırakmayın.",
            CweCode: "CWE-350",
            OwaspCategory: "A05:2021-Security Misconfiguration"));

        return findings;
    }

    private HttpClient CreateClient(CandidateEngineContext context)
    {
        var client = httpFactory.Create(TimeSpan.FromSeconds(12));
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(context.UserAgent);
        return client;
    }
}

/// <summary>
/// Pasif JS varlık taraması: ana sayfadaki <script src> dosyalarını indirip
/// yüksek güvenli sızıntı desenlerini (API key, token, iç endpoint) arar.
/// Tamamen pasif GET; hiçbir payload gönderilmez.
/// </summary>
public sealed class JsSecretExposureCandidateEngine(
    SecureHttpClientFactory httpFactory,
    ILogger<JsSecretExposureCandidateEngine> logger) : IApplicationSecurityCandidateEngine
{
    private sealed record SecretPattern(string Name, Regex Rx);

    private static readonly SecretPattern[] Patterns =
    [
        new("AWS Access Key", new Regex(@"AKIA[0-9A-Z]{16}", RegexOptions.Compiled)),
        new("Google API Key", new Regex(@"AIza[0-9A-Za-z\-_]{35}", RegexOptions.Compiled)),
        new("Slack Token", new Regex(@"xox[baprs]-[0-9A-Za-z\-]{10,48}", RegexOptions.Compiled)),
        new("Stripe Live Key", new Regex(@"sk_live_[0-9A-Za-z]{24,}", RegexOptions.Compiled)),
        new("GitHub Token", new Regex(@"gh[pousr]_[0-9A-Za-z]{36,}", RegexOptions.Compiled)),
        new("Private Key Block", new Regex(@"-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----", RegexOptions.Compiled)),
        new("JWT", new Regex(@"eyJ[A-Za-z0-9_\-]{10,}\.eyJ[A-Za-z0-9_\-]{10,}\.[A-Za-z0-9_\-]{10,}", RegexOptions.Compiled)),
        new("Generic Secret Assignment", new Regex(@"(?i)(api[_-]?key|secret|access[_-]?token|client[_-]?secret)['""]?\s*[:=]\s*['""][0-9A-Za-z\-_]{16,}['""]", RegexOptions.Compiled))
    ];

    public string EngineKey => "js-secret-exposure";

    public async Task<IReadOnlyList<CandidateFindingDraft>> RunAsync(
        CandidateEngineContext context,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<CandidateFindingDraft>();
        using var client = CreateClient(context);

        string homeHtml;
        try
        {
            using var homeResponse = await client.GetAsync(context.BaseUri, cancellationToken);
            homeHtml = await homeResponse.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Home fetch failed for JS secret scan {Host}", context.TargetHost);
            return findings;
        }

        var scripts = ExtractSameSiteScripts(homeHtml, context.BaseUri).Take(15).ToList();
        var scanned = 0;
        foreach (var scriptUrl in scripts)
        {
            if (scanned >= 12)
            {
                break;
            }

            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var response = await client.GetAsync(scriptUrl, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                scanned++;
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                if (content.Length > 2_000_000)
                {
                    content = content[..2_000_000];
                }

                foreach (var pattern in Patterns)
                {
                    var match = pattern.Rx.Match(content);
                    if (!match.Success)
                    {
                        continue;
                    }

                    findings.Add(new CandidateFindingDraft(
                        Title: $"Client-side secret exposure candidate: {pattern.Name}",
                        Description:
                            $"'{pattern.Name}' deseni istemci tarafı JS içinde bulundu. Gerçek/aktif secret olduğu ve " +
                            "yetki sağladığı manuel doğrulanmadan Submit edilmemeli (public key / test değeri olabilir).",
                        CheckCode: "asc.js-secret",
                        Fingerprint: $"asc.js-secret.{pattern.Name.Replace(' ', '-').ToLowerInvariant()}",
                        Severity: Severity.Medium,
                        Category: "Information Disclosure",
                        AffectedUrl: scriptUrl.ToString(),
                        AffectedParameter: null,
                        Evidence: $"Pattern={pattern.Name}; Asset={scriptUrl.AbsolutePath}; MatchIndex={match.Index} [value redacted]",
                        Remediation: "Sırları istemci paketlerinden çıkarın; sunucu tarafına taşıyın; sızan anahtarları rotate edin.",
                        CweCode: "CWE-615",
                        OwaspCategory: "A05:2021-Security Misconfiguration"));
                    break;
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "JS asset fetch failed for {Url}", scriptUrl);
            }
        }

        return findings;
    }

    private static IReadOnlyList<Uri> ExtractSameSiteScripts(string html, Uri baseUri)
    {
        var result = new List<Uri>();
        if (string.IsNullOrWhiteSpace(html))
        {
            return result;
        }

        var document = new HtmlParser().ParseDocument(html);
        foreach (var script in document.QuerySelectorAll("script[src]"))
        {
            var src = script.GetAttribute("src");
            if (string.IsNullOrWhiteSpace(src)
                || !Uri.TryCreate(baseUri, src, out var absolute)
                || (absolute.Scheme != Uri.UriSchemeHttps && absolute.Scheme != Uri.UriSchemeHttp))
            {
                continue;
            }

            var baseHost = baseUri.Host.TrimStart('.').ToLowerInvariant();
            var scriptHost = absolute.Host.TrimStart('.').ToLowerInvariant();
            if (scriptHost != baseHost && !scriptHost.EndsWith("." + baseHost, StringComparison.Ordinal))
            {
                continue;
            }

            if (!result.Contains(absolute))
            {
                result.Add(absolute);
            }
        }

        return result;
    }

    private HttpClient CreateClient(CandidateEngineContext context)
    {
        var client = httpFactory.Create(TimeSpan.FromSeconds(15));
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(context.UserAgent);
        return client;
    }
}

/// <summary>
/// GraphQL / API yüzey tespiti: yaygın API dokümantasyon ve GraphQL uçlarının
/// herkese açık olup olmadığını (introspection, playground, swagger) tespit eder.
/// Yalnızca GET + tek minimal introspection sorgusu; veri değiştiren işlem yok.
/// </summary>
public sealed class ApiSurfaceCandidateEngine(
    SecureHttpClientFactory httpFactory,
    ILogger<ApiSurfaceCandidateEngine> logger) : IApplicationSecurityCandidateEngine
{
    private static readonly string[] GraphQlPaths = ["/graphql", "/api/graphql", "/v1/graphql", "/query"];
    private static readonly string[] DocPaths =
    [
        "/swagger/v1/swagger.json", "/swagger.json", "/openapi.json", "/api-docs",
        "/api/swagger.json", "/.well-known/openapi.json", "/graphql/console", "/playground", "/altair"
    ];

    public string EngineKey => "api-surface";

    public async Task<IReadOnlyList<CandidateFindingDraft>> RunAsync(
        CandidateEngineContext context,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<CandidateFindingDraft>();
        using var client = CreateClient(context);

        foreach (var path in DocPaths)
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
                var looksLikeSpec = body.Contains("\"swagger\"", StringComparison.OrdinalIgnoreCase)
                    || body.Contains("\"openapi\"", StringComparison.OrdinalIgnoreCase)
                    || body.Contains("GraphQL Playground", StringComparison.OrdinalIgnoreCase)
                    || body.Contains("graphiql", StringComparison.OrdinalIgnoreCase)
                    || body.Contains("altair", StringComparison.OrdinalIgnoreCase);
                if (!looksLikeSpec)
                {
                    continue;
                }

                findings.Add(new CandidateFindingDraft(
                    Title: $"Publicly exposed API surface: {path}",
                    Description:
                        "Herkese açık API dokümantasyonu/konsolu erişilebilir. Kendisi zafiyet değildir; " +
                        "saldırı yüzeyini artırır ve hassas uçlar sızabilir. Manuel değerlendirme önerilir.",
                    CheckCode: "asc.api-surface",
                    Fingerprint: "asc.api-surface.doc-exposed",
                    Severity: Severity.Low,
                    Category: "Information Disclosure",
                    AffectedUrl: url.ToString(),
                    AffectedParameter: null,
                    Evidence: $"HTTP 200 on {path}; length={body.Length}",
                    Remediation: "Prod ortamında API konsollarını/spec dosyalarını kimlik doğrulama arkasına alın veya kaldırın.",
                    CweCode: "CWE-200",
                    OwaspCategory: "A05:2021-Security Misconfiguration"));
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "API doc probe failed for {Path}", path);
            }
        }

        foreach (var path in GraphQlPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var url = new Uri(context.BaseUri, path);
                const string introspection = "{\"query\":\"{__schema{queryType{name}}}\"}";
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(introspection, System.Text.Encoding.UTF8, "application/json")
                };
                using var response = await client.SendAsync(request, cancellationToken);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    continue;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!body.Contains("__schema", StringComparison.OrdinalIgnoreCase)
                    && !body.Contains("queryType", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                findings.Add(new CandidateFindingDraft(
                    Title: $"GraphQL introspection enabled: {path}",
                    Description:
                        "GraphQL introspection herkese açık; tüm şema (tipler, alanlar, mutasyonlar) numaralandırılabilir. " +
                        "Prod'da genelde kapatılması önerilir. Etki manuel değerlendirilmeli.",
                    CheckCode: "asc.api-surface",
                    Fingerprint: "asc.api-surface.graphql-introspection",
                    Severity: Severity.Low,
                    Category: "Information Disclosure",
                    AffectedUrl: url.ToString(),
                    AffectedParameter: null,
                    Evidence: $"Introspection minimal query returned __schema on {path}.",
                    Remediation: "Prod ortamında GraphQL introspection'ı devre dışı bırakın; hassas mutasyonları yetkilendirin.",
                    CweCode: "CWE-200",
                    OwaspCategory: "A05:2021-Security Misconfiguration"));
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "GraphQL introspection probe failed for {Path}", path);
            }
        }

        return findings;
    }

    private HttpClient CreateClient(CandidateEngineContext context)
    {
        var client = httpFactory.Create(TimeSpan.FromSeconds(12));
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(context.UserAgent);
        return client;
    }
}

/// <summary>
/// Open redirect adayı: yaygın yönlendirme parametrelerine site-dışı zararsız
/// bir işaret değeri konur ve Location başlığı bu değeri döndürüyor mu bakılır.
/// Exploit değil; yalnızca yönlendirme davranışının tespiti (redirect takip edilmez).
/// </summary>
public sealed class OpenRedirectCandidateEngine(
    SecureHttpClientFactory httpFactory,
    ILogger<OpenRedirectCandidateEngine> logger) : IApplicationSecurityCandidateEngine
{
    private const string Canary = "https://example.com/redirect-canary";
    private static readonly string[] Params = ["next", "url", "redirect", "return", "returnUrl", "dest", "destination", "continue", "r", "u"];

    public string EngineKey => "open-redirect";

    public async Task<IReadOnlyList<CandidateFindingDraft>> RunAsync(
        CandidateEngineContext context,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<CandidateFindingDraft>();
        using var client = CreateClient(context);

        foreach (var param in Params)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var builder = new UriBuilder(context.BaseUri) { Query = $"{param}={Uri.EscapeDataString(Canary)}" };
                using var response = await client.GetAsync(builder.Uri, cancellationToken);
                if (response.StatusCode is not (HttpStatusCode.Redirect or HttpStatusCode.MovedPermanently
                    or HttpStatusCode.Found or HttpStatusCode.SeeOther or HttpStatusCode.TemporaryRedirect
                    or HttpStatusCode.PermanentRedirect))
                {
                    continue;
                }

                var location = response.Headers.Location?.ToString();
                if (string.IsNullOrWhiteSpace(location))
                {
                    continue;
                }

                if (!location.Contains("example.com/redirect-canary", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                findings.Add(new CandidateFindingDraft(
                    Title: $"Open redirect candidate — parameter '{param}'",
                    Description:
                        $"'{param}' parametresi site-dışı bir adrese yönlendirme (Location) üretti. " +
                        "Phishing/token sızıntısı etkisi manuel doğrulanmalı.",
                    CheckCode: "asc.open-redirect",
                    Fingerprint: "asc.open-redirect.location-reflection",
                    Severity: Severity.Low,
                    Category: "Open Redirect",
                    AffectedUrl: builder.Uri.ToString(),
                    AffectedParameter: param,
                    Evidence: $"Status={(int)response.StatusCode}; Location={location}",
                    Remediation: "Yönlendirme hedeflerini allowlist ile sınırlayın; yalnızca göreli yol veya bilinen host kabul edin.",
                    CweCode: "CWE-601",
                    OwaspCategory: "A01:2021-Broken Access Control"));
                break;
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Open redirect probe failed for {Param}", param);
            }
        }

        return findings;
    }

    private HttpClient CreateClient(CandidateEngineContext context)
    {
        // Redirect takip edilmez: Location başlığını görmek istiyoruz.
        var client = httpFactory.Create(TimeSpan.FromSeconds(12), maxRedirects: 0, allowRedirects: false);
        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(context.UserAgent);
        return client;
    }
}
