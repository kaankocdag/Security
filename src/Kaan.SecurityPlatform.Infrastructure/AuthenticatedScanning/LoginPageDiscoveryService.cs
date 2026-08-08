using AngleSharp.Html.Parser;
using Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning;

namespace Kaan.SecurityPlatform.Infrastructure.AuthenticatedScanning;

public sealed class LoginPageDiscoveryService : ILoginPageDiscoveryService
{
    private static readonly string[] HrefKeywords =
    [
        "login", "signin", "sign-in", "log-in", "auth", "sso", "account/login",
        "oturum-ac", "oturumac", "giris", "uye-girisi", "hesap/giris"
    ];

    private static readonly string[] TextKeywords =
    [
        "login", "log in", "sign in", "signin", "my account",
        "giriş yap", "giris yap", "oturum aç", "oturum ac", "üye girişi", "uye girisi", "hesabım"
    ];

    private static readonly (string Provider, string[] Signals)[] OAuthSignals =
    [
        ("Google", ["accounts.google.com", "with google", "google ile", "gsi/client", "data-provider=\"google\""]),
        ("Microsoft", ["login.microsoftonline.com", "with microsoft", "microsoft ile", "login.live.com"]),
        ("Apple", ["appleid.apple.com", "with apple", "apple ile"]),
        ("GitHub", ["github.com/login/oauth", "with github", "github ile"]),
        ("Facebook", ["facebook.com/v", "with facebook", "facebook ile", "connect.facebook.net"]),
        ("Okta", ["okta.com", "oktapreview.com"]),
        ("Auth0", ["auth0.com"]),
        ("SAML", ["samlrequest", "/saml2/", "saml/sso"])
    ];

    public IReadOnlyList<string> ExtractLoginLinks(string html, string pageUrl)
    {
        if (string.IsNullOrWhiteSpace(html) || !Uri.TryCreate(pageUrl, UriKind.Absolute, out var basePage))
        {
            return [];
        }

        var document = new HtmlParser().ParseDocument(html);
        var results = new List<string>();

        foreach (var anchor in document.QuerySelectorAll("a[href]"))
        {
            var href = anchor.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href) || href.StartsWith('#')
                || href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!Uri.TryCreate(basePage, href, out var absolute)
                || (absolute.Scheme != Uri.UriSchemeHttps && absolute.Scheme != Uri.UriSchemeHttp))
            {
                continue;
            }

            // Kimlik bilgisi hedefi olarak yalnızca hedef alan adı ve alt alanları önerilir.
            if (!IsSameSite(basePage.Host, absolute.Host))
            {
                continue;
            }

            var hrefHit = HrefKeywords.Any(k => absolute.PathAndQuery.Contains(k, StringComparison.OrdinalIgnoreCase));
            var text = (anchor.TextContent ?? string.Empty).Trim();
            var label = string.IsNullOrEmpty(text)
                ? anchor.GetAttribute("aria-label") ?? anchor.GetAttribute("title") ?? string.Empty
                : text;
            var textHit = label.Length <= 40
                && TextKeywords.Any(k => label.Contains(k, StringComparison.OrdinalIgnoreCase));

            if (!hrefHit && !textHit)
            {
                continue;
            }

            var normalized = absolute.GetLeftPart(UriPartial.Query);
            if (!results.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                results.Add(normalized);
            }

            if (results.Count >= 8)
            {
                break;
            }
        }

        return results;
    }

    public IReadOnlyList<string> DetectOAuthProviders(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        return OAuthSignals
            .Where(s => s.Signals.Any(sig => html.Contains(sig, StringComparison.OrdinalIgnoreCase)))
            .Select(s => s.Provider)
            .ToList();
    }

    public bool HasPasswordForm(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return false;
        }

        var document = new HtmlParser().ParseDocument(html);
        return document.QuerySelectorAll("input[type=password]").Length > 0;
    }

    private static bool IsSameSite(string baseHost, string candidateHost)
    {
        var a = baseHost.TrimStart('.').ToLowerInvariant();
        var b = candidateHost.TrimStart('.').ToLowerInvariant();
        if (a == b)
        {
            return true;
        }

        var registrable = Registrable(a);
        return b == registrable || b.EndsWith("." + registrable, StringComparison.Ordinal);
    }

    private static string Registrable(string host)
    {
        var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length <= 2 ? host : string.Join('.', parts[^2..]);
    }
}
