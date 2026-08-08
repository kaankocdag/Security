using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Infrastructure.AuthenticatedScanning;

public sealed class LoginPageDetector : ILoginPageDetector
{
    private static readonly string[] LoginPaths = ["/login", "/signin", "/sign-in", "/account/login", "/auth/login", "/oturum-ac"];

    public bool LooksLikeLoginPage(string? url, string? html, string? title)
    {
        var urlHit = url is not null && LoginPaths.Any(p => url.Contains(p, StringComparison.OrdinalIgnoreCase));
        var titleHit = title is not null && (
            title.Contains("login", StringComparison.OrdinalIgnoreCase)
            || title.Contains("sign in", StringComparison.OrdinalIgnoreCase)
            || title.Contains("oturum", StringComparison.OrdinalIgnoreCase));
        var htmlHit = !string.IsNullOrEmpty(html) && (
            html.Contains("type=\"password\"", StringComparison.OrdinalIgnoreCase)
            || html.Contains("type='password'", StringComparison.OrdinalIgnoreCase));
        var textHit = !string.IsNullOrEmpty(html) && (
            html.Contains("sign in", StringComparison.OrdinalIgnoreCase)
            || html.Contains("log in", StringComparison.OrdinalIgnoreCase)
            || html.Contains("oturum aç", StringComparison.OrdinalIgnoreCase));
        return urlHit || titleHit || (htmlHit && textHit) || (htmlHit && urlHit);
    }

    public string? SuggestLoginPath(IEnumerable<string> discoveredPaths) =>
        discoveredPaths.FirstOrDefault(p => LoginPaths.Any(l => p.Contains(l, StringComparison.OrdinalIgnoreCase)))
        ?? "/login";
}

public sealed class RegistrationPageDetector : IRegistrationPageDetector
{
    public IReadOnlyList<string> CandidatePaths { get; } =
    [
        "/register", "/signup", "/sign-up", "/create-account", "/kayit", "/uye-ol", "/account/register"
    ];

    public bool LooksLikeRegistrationPage(string? url, string? html, string? title)
    {
        var urlHit = url is not null && CandidatePaths.Any(p => url.Contains(p, StringComparison.OrdinalIgnoreCase));
        var titleHit = title is not null && (
            title.Contains("register", StringComparison.OrdinalIgnoreCase)
            || title.Contains("sign up", StringComparison.OrdinalIgnoreCase)
            || title.Contains("kayıt", StringComparison.OrdinalIgnoreCase)
            || title.Contains("üye ol", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrEmpty(html))
        {
            return urlHit || titleHit;
        }

        var lower = html.ToLowerInvariant();
        var hasPassword = lower.Contains("type=\"password\"") || lower.Contains("type='password'");
        var hasEmail = lower.Contains("type=\"email\"") || lower.Contains("name=\"email\"") || lower.Contains("autocomplete=\"email\"");
        var hasSignupText = lower.Contains("sign up") || lower.Contains("register") || lower.Contains("create account")
                            || lower.Contains("üye ol") || lower.Contains("kayıt ol");
        return urlHit || titleHit || (hasPassword && hasEmail && hasSignupText);
    }
}

public sealed class ManualTakeoverService : IManualTakeoverService
{
    public ManualTakeoverReason Detect(string? url, string? html)
    {
        if (string.IsNullOrEmpty(html) && string.IsNullOrEmpty(url))
        {
            return ManualTakeoverReason.None;
        }

        var blob = $"{url}\n{html}".ToLowerInvariant();
        if (blob.Contains("captcha") || blob.Contains("recaptcha") || blob.Contains("hcaptcha") || blob.Contains("cf-turnstile"))
        {
            return ManualTakeoverReason.Captcha;
        }

        if (blob.Contains("mfa") || blob.Contains("two-factor") || blob.Contains("2fa") || blob.Contains("one-time") || blob.Contains("otp"))
        {
            return ManualTakeoverReason.Mfa;
        }

        if (blob.Contains("verify your email") || blob.Contains("email verification") || blob.Contains("doğrulama kodu"))
        {
            return ManualTakeoverReason.EmailVerification;
        }

        if (blob.Contains("phone verification") || blob.Contains("sms code") || blob.Contains("telefon doğrula"))
        {
            return ManualTakeoverReason.PhoneVerification;
        }

        // Yalnızca aktif OAuth yönlendirme / buton sinyali — script URL'lerinde geçen "oauth" false-positive olmasın.
        if (blob.Contains("accounts.google.com") || blob.Contains("login.microsoftonline.com")
            || blob.Contains("github.com/login/oauth") || blob.Contains("continue with google")
            || blob.Contains("sign in with google") || blob.Contains("sign in with microsoft"))
        {
            return ManualTakeoverReason.OAuth;
        }

        if (blob.Contains("checkout") || blob.Contains("payment") || blob.Contains("/billing") || blob.Contains("credit card") || blob.Contains("subscribe now"))
        {
            return ManualTakeoverReason.PaymentOrSubscription;
        }

        if (blob.Contains("unusual traffic") || blob.Contains("bot detection") || blob.Contains("are you a robot"))
        {
            return ManualTakeoverReason.BotProtection;
        }

        return ManualTakeoverReason.None;
    }

    public string UserMessage(ManualTakeoverReason reason) =>
        reason == ManualTakeoverReason.None
            ? string.Empty
            : "Manuel işlem gerekli. Tarayıcı kontrolü size bırakıldı. İşlemi tamamladıktan sonra ‘Devam Et’ düğmesine basın.";
}

public sealed class RegistrationFormAnalyzer : IRegistrationFormAnalyzer
{
    public RegistrationFormAnalysis Analyze(string html, string pageUrl)
    {
        var takeover = new ManualTakeoverService().Detect(pageUrl, html);
        var parser = new HtmlParser();
        var doc = parser.ParseDocument(html ?? string.Empty);
        var form = doc.QuerySelectorAll("form").FirstOrDefault(f =>
        {
            var t = f.TextContent.ToLowerInvariant() + (f.OuterHtml.ToLowerInvariant());
            return t.Contains("password") && (t.Contains("email") || t.Contains("user") || t.Contains("sign up") || t.Contains("register"));
        }) ?? doc.QuerySelector("form");

        if (form is null)
        {
            return new RegistrationFormAnalysis(false, null, string.Empty, [], takeover, false, false);
        }

        var action = form.GetAttribute("action") ?? pageUrl;
        var actionHost = TryHost(action, pageUrl);
        var fields = new List<RegistrationFormField>();
        foreach (var input in form.QuerySelectorAll("input, select, textarea"))
        {
            var type = (input.GetAttribute("type") ?? "text").ToLowerInvariant();
            var name = input.GetAttribute("name") ?? string.Empty;
            var id = input.GetAttribute("id") ?? string.Empty;
            var placeholder = input.GetAttribute("placeholder") ?? string.Empty;
            var autocomplete = input.GetAttribute("autocomplete") ?? string.Empty;
            var aria = input.GetAttribute("aria-label") ?? string.Empty;
            var label = FindLabel(doc, id, name);
            var blob = $"{type} {name} {id} {placeholder} {autocomplete} {aria} {label}".ToLowerInvariant();
            var required = input.HasAttribute("required") || blob.Contains("required");

            var kind = Classify(blob, type);
            if (kind == RegistrationFormFieldKind.Checkbox && type != "checkbox")
            {
                continue;
            }

            fields.Add(new RegistrationFormField(kind, !string.IsNullOrEmpty(name) ? $"name={name}" : $"id={id}", required, label));
        }

        var hasNewsletter = fields.Any(f => f.Kind == RegistrationFormFieldKind.NewsletterConsent);
        var hasTerms = fields.Any(f => f.Kind == RegistrationFormFieldKind.TermsAcceptance);
        if (fields.Any(f => f.Kind == RegistrationFormFieldKind.UnknownRequiredField && f.Required)
            && takeover == ManualTakeoverReason.None)
        {
            takeover = ManualTakeoverReason.UnknownRequiredField;
        }

        if (hasTerms && takeover == ManualTakeoverReason.None)
        {
            takeover = ManualTakeoverReason.TermsAcceptance;
        }

        return new RegistrationFormAnalysis(true, action, actionHost, fields, takeover, hasNewsletter, hasTerms);
    }

    private static RegistrationFormFieldKind Classify(string blob, string type)
    {
        if (type is "password" || blob.Contains("password"))
        {
            return blob.Contains("confirm") || blob.Contains("repeat")
                ? RegistrationFormFieldKind.ConfirmPassword
                : RegistrationFormFieldKind.Password;
        }

        if (type == "email" || blob.Contains("email") || blob.Contains("e-mail"))
        {
            return RegistrationFormFieldKind.Email;
        }

        if (blob.Contains("username") || blob.Contains("user name") || blob.Contains("kullanıcı"))
        {
            return RegistrationFormFieldKind.Username;
        }

        if (blob.Contains("first") && blob.Contains("name") || blob.Contains("adınız") || blob.Contains("firstname"))
        {
            return RegistrationFormFieldKind.FirstName;
        }

        if (blob.Contains("last") && blob.Contains("name") || blob.Contains("soyad") || blob.Contains("lastname"))
        {
            return RegistrationFormFieldKind.LastName;
        }

        if (blob.Contains("display") || blob.Contains("full name") || blob.Contains("görünen"))
        {
            return RegistrationFormFieldKind.DisplayName;
        }

        if (blob.Contains("country") || blob.Contains("ülke"))
        {
            return RegistrationFormFieldKind.Country;
        }

        if (blob.Contains("birth") || blob.Contains("doğum") || type == "date")
        {
            return RegistrationFormFieldKind.BirthDate;
        }

        if (type == "checkbox")
        {
            if (blob.Contains("newsletter") || blob.Contains("marketing") || blob.Contains("kampanya") || blob.Contains("promo"))
            {
                return RegistrationFormFieldKind.NewsletterConsent;
            }

            if (blob.Contains("terms") || blob.Contains("privacy") || blob.Contains("koşul") || blob.Contains("sözleşme") || blob.Contains("agree"))
            {
                return RegistrationFormFieldKind.TermsAcceptance;
            }

            return RegistrationFormFieldKind.Checkbox;
        }

        if (blob.Contains("captcha"))
        {
            return RegistrationFormFieldKind.Captcha;
        }

        if (blob.Contains("otp") || blob.Contains("mfa") || blob.Contains("2fa"))
        {
            return RegistrationFormFieldKind.Mfa;
        }

        if (blob.Contains("verification") || blob.Contains("doğrulama"))
        {
            return RegistrationFormFieldKind.VerificationCode;
        }

        return type is "hidden" or "submit" or "button"
            ? RegistrationFormFieldKind.Checkbox
            : RegistrationFormFieldKind.UnknownRequiredField;
    }

    private static string? FindLabel(AngleSharp.Html.Dom.IHtmlDocument doc, string id, string name)
    {
        if (!string.IsNullOrEmpty(id))
        {
            var byFor = doc.QuerySelector($"label[for='{id}']");
            if (byFor is not null)
            {
                return byFor.TextContent.Trim();
            }
        }

        return null;
    }

    private static string TryHost(string action, string pageUrl)
    {
        try
        {
            var baseUri = new Uri(pageUrl, UriKind.Absolute);
            var uri = Uri.TryCreate(action, UriKind.Absolute, out var abs) ? abs : new Uri(baseUri, action);
            return uri.Host;
        }
        catch
        {
            return string.Empty;
        }
    }
}

public sealed class LoginFormAnalyzer : ILoginFormAnalyzer
{
    public LoginFormAnalysis Analyze(string html, string pageUrl)
    {
        var takeover = new ManualTakeoverService().Detect(pageUrl, html);
        var parser = new HtmlParser();
        var doc = parser.ParseDocument(html ?? string.Empty);
        var form = doc.QuerySelectorAll("form").FirstOrDefault(f =>
            f.OuterHtml.Contains("password", StringComparison.OrdinalIgnoreCase));
        if (form is null)
        {
            return new LoginFormAnalysis(false, null, string.Empty, false, false, takeover);
        }

        var action = form.GetAttribute("action") ?? pageUrl;
        var host = string.Empty;
        try
        {
            var baseUri = new Uri(pageUrl);
            host = (Uri.TryCreate(action, UriKind.Absolute, out var a) ? a : new Uri(baseUri, action)).Host;
        }
        catch { /* ignore */ }

        var hasPassword = form.QuerySelector("input[type='password']") is not null;
        var hasUser = form.QuerySelector("input[type='email'], input[name*='user' i], input[name*='email' i], input[autocomplete='username']") is not null;
        return new LoginFormAnalysis(true, action, host, hasPassword, hasUser, takeover);
    }
}

public sealed class AuthenticationStateDetector : IAuthenticationStateDetector
{
    private readonly ILoginPageDetector _login = new LoginPageDetector();

    public bool IsAuthenticated(string? url, string? html, int statusCode)
    {
        if (statusCode is 401 or 403)
        {
            return false;
        }

        if (_login.LooksLikeLoginPage(url, html, null))
        {
            return false;
        }

        if (string.IsNullOrEmpty(html))
        {
            return false;
        }

        var lower = html.ToLowerInvariant();
        return lower.Contains("logout") || lower.Contains("sign out") || lower.Contains("log out")
               || lower.Contains("signed in as") || lower.Contains("hesabım") || lower.Contains("profilim");
    }

    public bool IsAuthRequired(string? url, string? html, int statusCode, IReadOnlyList<string> redirectChain)
    {
        if (statusCode is 401 or 403)
        {
            return true;
        }

        if (redirectChain.Any(r => r.Contains("/login", StringComparison.OrdinalIgnoreCase)
                                   || r.Contains("/signin", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return _login.LooksLikeLoginPage(url, html, null)
               || (!string.IsNullOrEmpty(html) && (
                   html.Contains("Sign in", StringComparison.OrdinalIgnoreCase)
                   || html.Contains("Log in", StringComparison.OrdinalIgnoreCase)
                   || html.Contains("Oturum aç", StringComparison.OrdinalIgnoreCase)));
    }
}

public sealed class AuthenticatedEvidenceRedactor : IAuthenticatedEvidenceRedactor
{
    private static readonly Regex Secrets = new(
        @"(?i)(password|passwd|authorization|cookie|token|bearer|refresh|otp|verification[_\s-]?code)\s*[:=]\s*\S+|Bearer\s+\S+|([a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,})",
        RegexOptions.Compiled);

    public string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var trimmed = value.Length > 600 ? value[..600] : value;
        return Secrets.Replace(trimmed, "[redacted]");
    }

    public string Hash(string? value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
        return Convert.ToHexString(bytes.AsSpan(0, 8)).ToLowerInvariant();
    }
}

public sealed class TestAccountVault : ITestAccountVault
{
    private readonly Application.Features.Validation.ITestAccountSecretProtector _protector;

    public TestAccountVault(Application.Features.Validation.ITestAccountSecretProtector protector)
    {
        _protector = protector;
    }

    public string ProtectPassword(string password) => _protector.Protect(password);

    public string UnprotectPassword(string encryptedReference) => _protector.Unprotect(encryptedReference);

    public string GenerateStrongPassword(int length = 24)
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*-_=+";
        var all = upper + lower + digits + special;
        var chars = new char[Math.Max(20, length)];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = special[RandomNumberGenerator.GetInt32(special.Length)];
        for (var i = 4; i < chars.Length; i++)
        {
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }

        // shuffle
        for (var i = chars.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
