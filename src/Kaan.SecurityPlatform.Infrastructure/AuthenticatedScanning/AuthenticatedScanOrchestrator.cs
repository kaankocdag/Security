using System.Net.Sockets;
using System.Text;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning;
using Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning.Dtos;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Kaan.SecurityPlatform.Application.Features.Validation;
using Kaan.SecurityPlatform.Domain.Entities.AuthenticatedScanning;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace Kaan.SecurityPlatform.Infrastructure.AuthenticatedScanning;

public sealed class AuthenticatedScanOptions
{
    public const string SectionName = "AuthenticatedScanning";
    public int MaxRequestsPerRun { get; set; } = 25;
    public int DelayMs { get; set; } = 400;
    public bool EnablePlaywright { get; set; } = true;
    public bool HeadlessDefault { get; set; }
    public int MaxTestAccountsPerTarget { get; set; } = 2;
}

public sealed class AuthenticatedScanOrchestrator(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IScopePolicyValidator scopeValidator,
    IAuthorizationEvidenceService authEvidence,
    ITestAccountVault vault,
    ILoginPageDetector loginDetector,
    IAuthenticationStateDetector authState,
    IAuthenticatedEvidenceRedactor redactor,
    IManualTakeoverService takeover,
    IAutomatedLoginService loginGate,
    IAuthenticatedCrawlService crawl,
    IScanSessionCleanupService sessionCleanup,
    ILoginPageDiscoveryService discovery,
    BrowserSessionHoldService browserHold,
    SecureHttpClientFactory httpFactory,
    IBugBountyAuditWriter audit,
    IHostEnvironment env,
    IOptions<AuthenticatedScanOptions> options,
    ILogger<AuthenticatedScanOrchestrator> logger) : IAuthenticatedScanOrchestrator
{
    private static readonly string[] DefaultPaths =
        ["/", "/admin", "/dashboard", "/account", "/settings", "/profile", "/api/me"];

    private static readonly string[] LoginProbePaths =
    [
        "/login", "/signin", "/sign-in", "/account/login", "/auth/login",
        "/users/sign_in", "/oturum-ac", "/giris"
    ];

    private static readonly string[] BlockPathFragments =
    [
        "/logout", "/delete", "/remove", "/unsubscribe", "/billing", "/checkout",
        "/purchase", "/payment", "/invite", "/send", "/publish", "/cancel-account"
    ];

    public async Task<Result<AuthScanPreconditionsDto>> GetPreconditionsAsync(
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        var mock = env.IsDevelopment();
        var scope = await scopeValidator.GetActiveAsync(targetId, cancellationToken);
        var auth = await authEvidence.GetActiveAsync(targetId, cancellationToken);
        var missing = new List<string>();
        if (!mock && scope is null) missing.Add("ScopePolicy");
        if (!mock && auth is null) missing.Add("AuthorizationEvidence");
        var count = await db.TargetTestAccounts.CountAsync(a => a.TargetId == targetId && a.IsActive, cancellationToken);
        var autoRegAllowed = scope is null
            || !scope.ProhibitedTestMethods.Contains("AUTO_REGISTRATION", StringComparison.OrdinalIgnoreCase);

        return Result<AuthScanPreconditionsDto>.Success(new AuthScanPreconditionsDto(
            targetId,
            scope is not null,
            auth is not null,
            scope?.TargetInBountyScope == true,
            autoRegAllowed,
            count,
            options.Value.MaxTestAccountsPerTarget,
            missing,
            "Yalnızca kendi güvenlik test hesabınız. Brute force/CAPTCHA atlatma/OAuth kişisel hesap yok. Giriş sonrası sayfa açılması açık değildir. Reward not guaranteed."));
    }

    public async Task<Result<LoginDiscoveryDto>> DiscoverLoginAsync(
        Guid targetId,
        CancellationToken cancellationToken = default)
    {
        var domain = await db.DomainAssets.AsNoTracking()
            .Where(d => d.Id == targetId)
            .Select(d => d.HostName)
            .FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(domain))
        {
            return Result<LoginDiscoveryDto>.Failure("not_found", "Hedef bulunamadı.");
        }

        var baseUri = new Uri($"https://{domain.TrimEnd('/')}/");
        var candidates = new List<string>();
        var providers = new List<string>();
        string? best = null;
        var passwordForm = false;

        using var client = CreateClient();

        var (homeHtml, _) = await SafeGetAsync(client, baseUri.ToString(), cancellationToken);
        if (homeHtml is not null)
        {
            candidates.AddRange(discovery.ExtractLoginLinks(homeHtml, baseUri.ToString()));
            providers.AddRange(discovery.DetectOAuthProviders(homeHtml));
        }

        foreach (var path in LoginProbePaths)
        {
            var absolute = new Uri(baseUri, path).ToString();
            if (!candidates.Contains(absolute, StringComparer.OrdinalIgnoreCase))
            {
                candidates.Add(absolute);
            }
        }

        foreach (var candidate in candidates.Take(8))
        {
            if (best is not null && passwordForm)
            {
                break;
            }

            var (html, status) = await SafeGetAsync(client, candidate, cancellationToken);
            if (html is null || status is >= 400)
            {
                continue;
            }

            var hasPassword = discovery.HasPasswordForm(html);
            var looksLikeLogin = hasPassword || loginDetector.LooksLikeLoginPage(candidate, html, null);
            if (!looksLikeLogin)
            {
                continue;
            }

            foreach (var provider in discovery.DetectOAuthProviders(html))
            {
                if (!providers.Contains(provider))
                {
                    providers.Add(provider);
                }
            }

            if (best is null || (hasPassword && !passwordForm))
            {
                best = candidate;
                passwordForm = hasPassword;
            }
        }

        var oauthOnly = !passwordForm && providers.Count > 0;
        var note = best is null
            ? "Login sayfası otomatik bulunamadı. URL'yi elle girip manuel giriş oturumu başlatabilirsiniz."
            : oauthOnly
                ? $"Sayfa yalnızca dış sağlayıcı ({string.Join(", ", providers)}) ile giriş sunuyor gibi görünüyor. Şifre otomasyonu yapılmaz; görünür tarayıcıda girişi siz tamamlarsınız."
                : "Şifre formu bulundu. İster kayıtlı test hesabıyla otomatik, ister manuel giriş oturumuyla devam edebilirsiniz.";

        return Result<LoginDiscoveryDto>.Success(new LoginDiscoveryDto(
            targetId, best, candidates.Take(8).ToList(), passwordForm, oauthOnly, providers, note));
    }

    /// <summary>
    /// Şifre otomasyonu yok: görünür tarayıcı açılır, giriş (Google/SSO/MFA dahil) kullanıcı
    /// tarafından yapılır, ardından "Devam Et" ile yalnızca güvenli GET probe'ları çalışır.
    /// </summary>
    public async Task<Result<AuthenticatedScanRunDto>> StartManualLoginSessionAsync(
        StartManualLoginSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.ExplicitUserApproval)
        {
            return Result<AuthenticatedScanRunDto>.Failure("approval_required", "Manuel giriş oturumu için açık onay zorunlu.");
        }

        var pre = await GetPreconditionsAsync(request.TargetId, cancellationToken);
        if (!pre.IsSuccess)
        {
            return Result<AuthenticatedScanRunDto>.Failure(pre.ErrorCode ?? "preconditions", pre.ErrorMessage ?? "Önkoşullar sağlanamadı.");
        }

        if (pre.Value!.MissingItems.Count > 0 && !env.IsDevelopment())
        {
            return Result<AuthenticatedScanRunDto>.Failure("preconditions", string.Join("; ", pre.Value.MissingItems));
        }

        var target = await db.DomainAssets.AsNoTracking()
            .Where(d => d.Id == request.TargetId)
            .Select(d => new { d.HostName, d.CompanyId })
            .FirstOrDefaultAsync(cancellationToken);
        if (target is null || string.IsNullOrWhiteSpace(target.HostName))
        {
            return Result<AuthenticatedScanRunDto>.Failure("not_found", "Hedef bulunamadı.");
        }

        var baseUri = new Uri($"https://{target.HostName.TrimEnd('/')}/");
        var loginUrl = ResolveLoginUrl(baseUri, request.LoginUrl);
        if (loginUrl is null)
        {
            return Result<AuthenticatedScanRunDto>.Failure(
                "login_url_out_of_scope",
                "Giriş adresi hedef alan adına ait olmalı.");
        }

        var run = new AuthenticatedScanRun
        {
            CompanyId = target.CompanyId,
            TargetId = request.TargetId,
            TestAccountId = null,
            Status = AuthenticatedScanRunStatus.LoggingIn,
            StartedAt = DateTime.UtcNow,
            RequestedBy = currentUser.UserId,
            UserApprovedAt = DateTime.UtcNow,
            MaxRequestCount = options.Value.MaxRequestsPerRun,
            HeadedBrowser = true
        };
        db.AuthenticatedScanRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("authscan.manual_session.started", "AuthenticatedScanRun", run.Id.ToString(),
            new { request.TargetId, LoginUrl = loginUrl }, cancellationToken);

        if (request.RunAnonymousBaseline)
        {
            var paths = DefaultPaths.Where(p => !crawl.IsPathBlocked(p)).ToList();
            using var anonClient = CreateClient();
            foreach (var path in paths)
            {
                if (run.ActualRequestCount >= run.MaxRequestCount) break;
                var obs = await ProbeAsync(anonClient, run, baseUri, path, false, null, cancellationToken);
                db.ScanModeObservations.Add(obs);
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        var opened = await OpenHeldBrowserAsync(run.Id, loginUrl, cancellationToken);

        run.Status = AuthenticatedScanRunStatus.AwaitingManualTakeover;
        run.TakeoverReason = ManualTakeoverReason.UserRequested;
        run.TakeoverMessage = opened
            ? "Görünür tarayıcı açıldı. Girişi kendiniz tamamlayın (Google/SSO/MFA dahil), sonra ‘Devam Et’e basın."
            : $"Tarayıcı açılamadı. {loginUrl} adresini kendi tarayıcınızda açıp giriş yapın, sonra ‘Devam Et’e basın.";
        run.LoginUrlUsed = loginUrl;
        if (!opened)
        {
            run.ErrorCode = "browser_launch_failed";
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result<AuthenticatedScanRunDto>.Success(await MapAsync(run.Id, cancellationToken));
    }

    public async Task<Result<AuthenticatedScanRunDto>> StartAuthenticatedScanAsync(
        StartAuthenticatedScanRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.ExplicitUserApproval)
        {
            return Result<AuthenticatedScanRunDto>.Failure("approval_required", "Girişli tarama için açık onay zorunlu.");
        }

        var pre = await GetPreconditionsAsync(request.TargetId, cancellationToken);
        if (pre.Value!.MissingItems.Count > 0 && !env.IsDevelopment())
        {
            return Result<AuthenticatedScanRunDto>.Failure("preconditions", string.Join("; ", pre.Value.MissingItems));
        }

        var account = await db.TargetTestAccounts.FirstOrDefaultAsync(
            a => a.Id == request.TestAccountId && a.TargetId == request.TargetId && a.IsActive, cancellationToken);
        if (account is null)
        {
            return Result<AuthenticatedScanRunDto>.Failure("account_not_found", "Aktif test hesabı bulunamadı.");
        }

        if (account.AccountStatus == TestAccountStatus.PendingVerification
            || account.VerificationStatus == TestAccountVerificationStatus.EmailPending)
        {
            return Result<AuthenticatedScanRunDto>.Failure(
                "pending_verification",
                "PendingVerification hesap otomatik girişte kullanılamaz.");
        }

        if (!account.OwnershipConfirmed || !account.TestingPermissionConfirmed)
        {
            return Result<AuthenticatedScanRunDto>.Failure("ownership_required", "Hesap ownership onayı eksik.");
        }

        var domain = await db.DomainAssets.AsNoTracking()
            .Where(d => d.Id == request.TargetId)
            .Select(d => d.HostName)
            .FirstAsync(cancellationToken);

        var run = new AuthenticatedScanRun
        {
            CompanyId = account.CompanyId,
            TargetId = request.TargetId,
            TestAccountId = account.Id,
            Status = AuthenticatedScanRunStatus.LoggingIn,
            StartedAt = DateTime.UtcNow,
            RequestedBy = currentUser.UserId,
            UserApprovedAt = DateTime.UtcNow,
            MaxRequestCount = options.Value.MaxRequestsPerRun,
            HeadedBrowser = request.HeadedBrowser
        };
        db.AuthenticatedScanRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("authscan.run.started", "AuthenticatedScanRun", run.Id.ToString(),
            new { request.TargetId, AccountLabel = account.Label }, cancellationToken);

        try
        {
            var baseUri = new Uri($"https://{domain.TrimEnd('/')}/");
            var paths = (request.PathsToProbe ?? DefaultPaths)
                .Where(p => !crawl.IsPathBlocked(p)
                            && !BlockPathFragments.Any(b => p.Contains(b, StringComparison.OrdinalIgnoreCase)))
                .Take(12)
                .ToList();

            // 1) Anonymous observations
            run.Status = AuthenticatedScanRunStatus.Scanning;
            await db.SaveChangesAsync(cancellationToken);
            var anonymous = new List<ScanModeObservation>();
            using (var anonClient = CreateClient())
            {
                foreach (var path in paths)
                {
                    if (run.ActualRequestCount >= run.MaxRequestCount) break;
                    var obs = await ProbeAsync(anonClient, run, baseUri, path, authenticated: false, null, cancellationToken);
                    anonymous.Add(obs);
                    db.ScanModeObservations.Add(obs);
                    if (obs.StatusCode == 429)
                    {
                        run.Status = AuthenticatedScanRunStatus.Stopped;
                        run.StopReason = "HTTP 429 — rate limited";
                        break;
                    }
                }
            }

            if (run.Status == AuthenticatedScanRunStatus.Stopped)
            {
                run.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                return Result<AuthenticatedScanRunDto>.Success(await MapAsync(run.Id, cancellationToken));
            }

            // 2) Login (Playwright when available; otherwise ManualTakeover guidance)
            run.Status = AuthenticatedScanRunStatus.LoggingIn;
            var loginUrl = account.LoginUrl
                           ?? new Uri(baseUri, loginDetector.SuggestLoginPath(paths) ?? "/login").ToString();
            run.LoginUrlUsed = loginUrl;

            string? password = null;
            try
            {
                password = vault.UnprotectPassword(account.EncryptedSecretReference);
            }
            catch
            {
                run.Status = AuthenticatedScanRunStatus.Failed;
                run.ErrorCode = "vault_error";
                run.ErrorMessage = "Test hesabı secret çözülemedi.";
                run.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                return Result<AuthenticatedScanRunDto>.Success(await MapAsync(run.Id, cancellationToken));
            }

            var loginResult = await TryLoginAsync(
                run.Id, domain, loginUrl, account.Email ?? account.Username ?? string.Empty, password,
                request.HeadedBrowser, cancellationToken);
            password = null; // never log / retain plaintext after login attempt

            if (loginResult.Takeover != ManualTakeoverReason.None)
            {
                run.Status = AuthenticatedScanRunStatus.AwaitingManualTakeover;
                run.TakeoverReason = loginResult.Takeover;
                run.ErrorCode = loginResult.BrowserHeld ? null : "browser_not_held";
                run.TakeoverMessage = loginResult.BrowserHeld
                    ? takeover.UserMessage(loginResult.Takeover)
                    : "Otomatik tarayıcı açılamadı veya hemen kapandı. Aşağıdaki ‘Tarayıcıda Aç’ ile login sayfasını kendi tarayıcınızda açın, girişi tamamlayın, sonra ‘Devam Et’e basın.";
                await db.SaveChangesAsync(cancellationToken);
                await audit.WriteAsync("authscan.manual_takeover", "AuthenticatedScanRun", run.Id.ToString(),
                    new { Reason = loginResult.Takeover.ToString(), loginResult.BrowserHeld }, cancellationToken);
                return Result<AuthenticatedScanRunDto>.Success(await MapAsync(run.Id, cancellationToken));
            }

            if (!loginResult.Success)
            {
                run.Status = AuthenticatedScanRunStatus.Failed;
                run.ErrorCode = "login_failed";
                run.ErrorMessage = "Giriş başarısız — brute force yapılmaz; tek kontrollü deneme.";
                run.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                await audit.WriteAsync("authscan.login.failed", "AuthenticatedScanRun", run.Id.ToString(),
                    new { account.Label }, cancellationToken);
                return Result<AuthenticatedScanRunDto>.Success(await MapAsync(run.Id, cancellationToken));
            }

            run.AuthenticationConfirmed = true;
            account.LastSuccessfulLoginAt = DateTime.UtcNow;
            await audit.WriteAsync("authscan.login.success", "AuthenticatedScanRun", run.Id.ToString(),
                new { account.Label }, cancellationToken);

            // 3) Authenticated probes with cookie header if provided (never logged)
            var authenticated = new List<ScanModeObservation>();
            using (var authClient = CreateClient())
            {
                if (!string.IsNullOrWhiteSpace(loginResult.CookieHeader))
                {
                    authClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", loginResult.CookieHeader);
                }

                foreach (var path in paths)
                {
                    if (run.ActualRequestCount >= run.MaxRequestCount) break;
                    var obs = await ProbeAsync(
                        authClient, run, baseUri, path, authenticated: true, account, cancellationToken);
                    authenticated.Add(obs);
                    db.ScanModeObservations.Add(obs);
                    if (obs.StatusCode == 429)
                    {
                        run.Status = AuthenticatedScanRunStatus.Stopped;
                        run.StopReason = "HTTP 429 — rate limited";
                        break;
                    }
                }
            }

            // 4) Compare
            foreach (var pair in anonymous.Zip(authenticated))
            {
                pair.Second.ComparisonResult = Compare(pair.First, pair.Second);
            }

            account.LastAuthenticatedScanAt = DateTime.UtcNow;
            run.Status = run.Status == AuthenticatedScanRunStatus.Stopped
                ? AuthenticatedScanRunStatus.Stopped
                : AuthenticatedScanRunStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;
            var cookieHeader = loginResult.CookieHeader;
            sessionCleanup.ClearInMemorySecrets(ref password, ref cookieHeader);
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("authscan.run.completed", "AuthenticatedScanRun", run.Id.ToString(),
                new { run.ActualRequestCount, run.AuthenticationConfirmed }, cancellationToken);

            return Result<AuthenticatedScanRunDto>.Success(await MapAsync(run.Id, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Authenticated scan failed for {TargetId}", request.TargetId);
            run.Status = AuthenticatedScanRunStatus.Failed;
            run.ErrorCode = "scan_exception";
            run.ErrorMessage = "Girişli tarama güvenli biçimde durduruldu.";
            run.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return Result<AuthenticatedScanRunDto>.Success(await MapAsync(run.Id, cancellationToken));
        }
    }

    /// <summary>
    /// Çerez tabanlı oturum: kullanıcı kendi tarayıcısında giriş yapıp oturum
    /// çerezini yapıştırır. Otomasyon yok; yalnızca güvenli GET probe'ları koşar.
    /// Çerez kalıcı olarak saklanmaz.
    /// </summary>
    public async Task<Result<AuthenticatedScanRunDto>> StartCookieSessionScanAsync(
        StartCookieSessionScanRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.ExplicitUserApproval)
        {
            return Result<AuthenticatedScanRunDto>.Failure("approval_required", "Çerez oturumu için açık onay zorunlu.");
        }

        var cookieHeader = NormalizeCookieData(request.CookieData);
        if (string.IsNullOrWhiteSpace(cookieHeader))
        {
            return Result<AuthenticatedScanRunDto>.Failure(
                "cookie_required",
                "Geçerli bir oturum çerezi yapıştırın (ham başlık veya Cookie-Editor JSON).");
        }

        var pre = await GetPreconditionsAsync(request.TargetId, cancellationToken);
        if (pre.Value!.MissingItems.Count > 0 && !env.IsDevelopment())
        {
            return Result<AuthenticatedScanRunDto>.Failure("preconditions", string.Join("; ", pre.Value.MissingItems));
        }

        var target = await db.DomainAssets.AsNoTracking()
            .Where(d => d.Id == request.TargetId)
            .Select(d => new { d.HostName, d.CompanyId })
            .FirstOrDefaultAsync(cancellationToken);
        if (target is null || string.IsNullOrWhiteSpace(target.HostName))
        {
            return Result<AuthenticatedScanRunDto>.Failure("not_found", "Hedef bulunamadı.");
        }

        var baseUri = new Uri($"https://{target.HostName.TrimEnd('/')}/");
        var run = new AuthenticatedScanRun
        {
            CompanyId = target.CompanyId,
            TargetId = request.TargetId,
            TestAccountId = null,
            Status = AuthenticatedScanRunStatus.Scanning,
            StartedAt = DateTime.UtcNow,
            RequestedBy = currentUser.UserId,
            UserApprovedAt = DateTime.UtcNow,
            MaxRequestCount = options.Value.MaxRequestsPerRun,
            HeadedBrowser = false
        };
        db.AuthenticatedScanRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("authscan.cookie_session.started", "AuthenticatedScanRun", run.Id.ToString(),
            new { request.TargetId }, cancellationToken);

        try
        {
            var paths = DefaultPaths.Where(p => !crawl.IsPathBlocked(p)).Take(12).ToList();
            var anonymous = new List<ScanModeObservation>();
            var authenticated = new List<ScanModeObservation>();

            if (request.RunAnonymousBaseline)
            {
                using var anonClient = CreateClient();
                foreach (var path in paths)
                {
                    if (run.ActualRequestCount >= run.MaxRequestCount) break;
                    var obs = await ProbeAsync(anonClient, run, baseUri, path, false, null, cancellationToken);
                    anonymous.Add(obs);
                    db.ScanModeObservations.Add(obs);
                }
            }

            using (var authClient = CreateClient())
            {
                authClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookieHeader);
                foreach (var path in paths)
                {
                    if (run.ActualRequestCount >= run.MaxRequestCount) break;
                    var obs = await ProbeAsync(authClient, run, baseUri, path, true, null, cancellationToken);
                    authenticated.Add(obs);
                    db.ScanModeObservations.Add(obs);
                    if (obs.StatusCode == 429)
                    {
                        run.Status = AuthenticatedScanRunStatus.Stopped;
                        run.StopReason = "HTTP 429 — rate limited";
                        break;
                    }
                }
            }

            run.AuthenticationConfirmed = authenticated.Any(o => o.AuthenticationConfirmed)
                || authenticated.Any(o => o.StatusCode is >= 200 and < 300 && !o.LoginDetected);

            foreach (var pair in anonymous.Zip(authenticated))
            {
                pair.Second.ComparisonResult = Compare(pair.First, pair.Second);
            }

            run.Status = run.Status == AuthenticatedScanRunStatus.Stopped
                ? AuthenticatedScanRunStatus.Stopped
                : AuthenticatedScanRunStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;
            cookieHeader = null;
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("authscan.cookie_session.completed", "AuthenticatedScanRun", run.Id.ToString(),
                new { run.ActualRequestCount, run.AuthenticationConfirmed }, cancellationToken);

            return Result<AuthenticatedScanRunDto>.Success(await MapAsync(run.Id, cancellationToken));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cookie-session scan failed for {TargetId}", request.TargetId);
            run.Status = AuthenticatedScanRunStatus.Failed;
            run.ErrorCode = "scan_exception";
            run.ErrorMessage = "Çerez oturumlu tarama güvenli biçimde durduruldu.";
            run.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return Result<AuthenticatedScanRunDto>.Success(await MapAsync(run.Id, cancellationToken));
        }
    }

    /// <summary>
    /// Ham "ad=değer; ad2=değer2" başlığını ya da Cookie-Editor benzeri JSON
    /// dışa aktarımını ([{"name":..,"value":..}]) tek satırlık Cookie başlığına çevirir.
    /// </summary>
    internal static string? NormalizeCookieData(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        if (trimmed.StartsWith('[') || trimmed.StartsWith('{'))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(trimmed);
                var root = doc.RootElement;
                var array = root.ValueKind == System.Text.Json.JsonValueKind.Array
                    ? root
                    : root.TryGetProperty("cookies", out var inner) ? inner : default;
                if (array.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var parts = new List<string>();
                    foreach (var item in array.EnumerateArray())
                    {
                        if (item.TryGetProperty("name", out var n) && item.TryGetProperty("value", out var v))
                        {
                            var name = n.GetString();
                            var value = v.GetString();
                            if (!string.IsNullOrWhiteSpace(name) && value is not null)
                            {
                                parts.Add($"{name}={value}");
                            }
                        }
                    }

                    return parts.Count > 0 ? string.Join("; ", parts) : null;
                }
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }

        // Ham başlık: yeni satır/başlık öneki temizliği.
        var cleaned = trimmed;
        if (cleaned.StartsWith("Cookie:", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned["Cookie:".Length..].Trim();
        }

        cleaned = cleaned.Replace("\r", " ").Replace("\n", " ").Trim();
        return cleaned.Contains('=') ? cleaned : null;
    }

    public async Task<Result<AuthenticatedScanRunDto>> GetRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var dto = await MapAsync(runId, cancellationToken);
        return dto is null
            ? Result<AuthenticatedScanRunDto>.Failure("not_found", "Çalışma bulunamadı.")
            : Result<AuthenticatedScanRunDto>.Success(dto);
    }

    public async Task<Result<AuthenticatedScanRunDto>> StopAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await db.AuthenticatedScanRuns.FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        if (run is null)
        {
            return Result<AuthenticatedScanRunDto>.Failure("not_found", "Çalışma bulunamadı.");
        }

        run.Status = AuthenticatedScanRunStatus.Stopped;
        run.StopReason = "User stop";
        run.CompletedAt = DateTime.UtcNow;
        await browserHold.ReleaseAsync(runId);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("authscan.run.stopped", "AuthenticatedScanRun", runId.ToString(), null, cancellationToken);
        return Result<AuthenticatedScanRunDto>.Success(await MapAsync(runId, cancellationToken));
    }

    public async Task<Result<AuthenticatedScanRunDto>> ContinueAfterManualTakeoverAsync(
        Guid runId,
        CancellationToken cancellationToken = default)
    {
        var run = await db.AuthenticatedScanRuns
            .Include(r => r.Observations)
            .Include(r => r.TestAccount)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        if (run is null)
        {
            return Result<AuthenticatedScanRunDto>.Failure("not_found", "Çalışma bulunamadı.");
        }

        if (run.Status != AuthenticatedScanRunStatus.AwaitingManualTakeover)
        {
            return Result<AuthenticatedScanRunDto>.Failure("invalid_state", "Manuel takeover beklenmiyor.");
        }

        string? cookieHeader = null;
        var authConfirmed = false;

        if (browserHold.TryGet(runId, out var held) && held is not null)
        {
            try
            {
                await held.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                var html = await held.Page.ContentAsync();
                var stillBlocked = takeover.Detect(held.Page.Url, html);
                if (stillBlocked is ManualTakeoverReason.Captcha or ManualTakeoverReason.Mfa
                    or ManualTakeoverReason.PaymentOrSubscription)
                {
                    run.TakeoverReason = stillBlocked;
                    run.TakeoverMessage = takeover.UserMessage(stillBlocked);
                    await db.SaveChangesAsync(cancellationToken);
                    return Result<AuthenticatedScanRunDto>.Success(await MapAsync(runId, cancellationToken));
                }

                authConfirmed = authState.IsAuthenticated(held.Page.Url, html, 200)
                                && !loginDetector.LooksLikeLoginPage(held.Page.Url, html, null);
                if (authConfirmed)
                {
                    var cookies = await held.Context.CookiesAsync();
                    cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Held browser session unreadable for {RunId}", runId);
            }
            finally
            {
                await browserHold.ReleaseAsync(runId);
            }
        }
        else
        {
            // Kullanıcı kendi tarayıcısında girişi tamamladı — cookie paylaşımı yok; onay ile tamamla.
            authConfirmed = true;
        }

        run.TakeoverReason = ManualTakeoverReason.None;
        run.TakeoverMessage = null;
        run.ErrorCode = null;
        run.AuthenticationConfirmed = authConfirmed;

        if (run.TestAccount is not null && authConfirmed)
        {
            run.TestAccount.LastSuccessfulLoginAt = DateTime.UtcNow;
        }

        // Held session cookies ile güvenli GET probe'ları tamamla
        if (authConfirmed && !string.IsNullOrWhiteSpace(cookieHeader))
        {
            run.Status = AuthenticatedScanRunStatus.Scanning;
            await db.SaveChangesAsync(cancellationToken);

            var domain = await db.DomainAssets.AsNoTracking()
                .Where(d => d.Id == run.TargetId)
                .Select(d => d.HostName)
                .FirstAsync(cancellationToken);
            var baseUri = new Uri($"https://{domain.TrimEnd('/')}/");
            var paths = DefaultPaths.Where(p => !crawl.IsPathBlocked(p)).Take(12).ToList();
            var anonymous = run.Observations.Where(o => !o.IsAuthenticatedMode).ToList();
            var authenticated = new List<ScanModeObservation>();

            using (var authClient = CreateClient())
            {
                authClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", cookieHeader);
                foreach (var path in paths)
                {
                    if (run.ActualRequestCount >= run.MaxRequestCount) break;
                    var obs = await ProbeAsync(authClient, run, baseUri, path, true, run.TestAccount, cancellationToken);
                    authenticated.Add(obs);
                    db.ScanModeObservations.Add(obs);
                    if (obs.StatusCode == 429)
                    {
                        run.Status = AuthenticatedScanRunStatus.Stopped;
                        run.StopReason = "HTTP 429 — rate limited";
                        break;
                    }
                }
            }

            foreach (var pair in anonymous.Zip(authenticated))
            {
                pair.Second.ComparisonResult = Compare(pair.First, pair.Second);
            }

            if (run.TestAccount is not null)
            {
                run.TestAccount.LastAuthenticatedScanAt = DateTime.UtcNow;
            }

            cookieHeader = null;
        }

        run.Status = run.Status == AuthenticatedScanRunStatus.Stopped
            ? AuthenticatedScanRunStatus.Stopped
            : AuthenticatedScanRunStatus.Completed;
        run.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("authscan.manual_takeover.continued", "AuthenticatedScanRun", runId.ToString(),
            new { run.AuthenticationConfirmed }, cancellationToken);
        return Result<AuthenticatedScanRunDto>.Success(await MapAsync(runId, cancellationToken));
    }

    private async Task<ScanModeObservation> ProbeAsync(
        HttpClient client,
        AuthenticatedScanRun run,
        Uri baseUri,
        string path,
        bool authenticated,
        TargetTestAccount? account,
        CancellationToken cancellationToken)
    {
        if (options.Value.DelayMs > 0 && run.ActualRequestCount > 0)
        {
            await Task.Delay(options.Value.DelayMs, cancellationToken);
        }

        var url = new Uri(baseUri, path);
        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            run.ActualRequestCount++;
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (body.Length > 32_000) body = body[..32_000];
            var status = (int)response.StatusCode;
            var final = response.RequestMessage?.RequestUri?.ToString() ?? url.ToString();
            var login = loginDetector.LooksLikeLoginPage(final, body, null);
            var denied = status is 401 or 403;
            var authOk = authenticated && authState.IsAuthenticated(final, body, status);

            return new ScanModeObservation
            {
                AuthenticatedScanRunId = run.Id,
                IsAuthenticatedMode = authenticated,
                TestAccountId = account?.Id,
                MaskedAccountLabel = account is null ? null : MaskLabel(account.Label),
                Url = url.ToString(),
                StatusCode = status,
                FinalUrl = redactor.Redact(final),
                ContentType = response.Content.Headers.ContentType?.ToString(),
                ResponseHash = redactor.Hash(body),
                LoginDetected = login,
                AccessDeniedDetected = denied,
                AuthenticationConfirmed = authOk,
                RedactedEvidence = redactor.Redact(
                    $"status={status}; login={login}; denied={denied}; authConfirmed={authOk}; len={body.Length}")
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or SocketException)
        {
            run.ActualRequestCount++;
            logger.LogDebug(ex, "Probe failed for {Url}", url);
            return new ScanModeObservation
            {
                AuthenticatedScanRunId = run.Id,
                IsAuthenticatedMode = authenticated,
                TestAccountId = account?.Id,
                MaskedAccountLabel = account is null ? null : MaskLabel(account.Label),
                Url = url.ToString(),
                StatusCode = 0,
                FinalUrl = url.ToString(),
                LoginDetected = false,
                AccessDeniedDetected = false,
                AuthenticationConfirmed = false,
                RedactedEvidence = redactor.Redact($"probe_failed={ex.GetType().Name}; host={url.Host}")
            };
        }
    }

    private async Task<(bool Success, ManualTakeoverReason Takeover, string? CookieHeader, bool BrowserHeld)> TryLoginAsync(
        Guid runId,
        string domain,
        string loginUrl,
        string username,
        string password,
        bool headed,
        CancellationToken cancellationToken)
    {
        if (!options.Value.EnablePlaywright)
        {
            return (false, ManualTakeoverReason.UserRequested, null, false);
        }

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        IBrowserContext? context = null;
        try
        {
            playwright = await Playwright.CreateAsync();
            // Kullanıcı headed istediğinde her zaman görünür pencere.
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = !headed && options.Value.HeadlessDefault,
                SlowMo = headed ? 50 : 0
            });
            context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = env.IsDevelopment()
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(loginUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });
            var html = await page.ContentAsync();
            var reason = takeover.Detect(page.Url, html);
            if (reason != ManualTakeoverReason.None)
            {
                HoldBrowser(runId, playwright, browser, context, page);
                return (false, reason, null, true);
            }

            var formAnalysis = new LoginFormAnalyzer().Analyze(html, page.Url);
            if (!loginGate.IsCredentialDestinationAllowed(domain, page.Url, formAnalysis.FormActionHost))
            {
                HoldBrowser(runId, playwright, browser, context, page);
                return (false, ManualTakeoverReason.OAuth, null, true);
            }

            var email = page.Locator("input[type='email'], input[name='email'], input[autocomplete='username']").First;
            var user = page.Locator("input[name='username'], input[name='user'], input[type='text']").First;
            var pass = page.Locator("input[type='password']").First;
            if (await email.CountAsync() > 0)
            {
                await email.FillAsync(username);
            }
            else if (await user.CountAsync() > 0)
            {
                await user.FillAsync(username);
            }
            else
            {
                HoldBrowser(runId, playwright, browser, context, page);
                return (false, ManualTakeoverReason.UnknownRequiredField, null, true);
            }

            await pass.FillAsync(password);
            html = await page.ContentAsync();
            reason = takeover.Detect(page.Url, html);
            if (reason != ManualTakeoverReason.None)
            {
                HoldBrowser(runId, playwright, browser, context, page);
                return (false, reason, null, true);
            }

            await page.Locator("button[type='submit'], input[type='submit']").First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            html = await page.ContentAsync();
            reason = takeover.Detect(page.Url, html);
            if (reason != ManualTakeoverReason.None)
            {
                HoldBrowser(runId, playwright, browser, context, page);
                return (false, reason, null, true);
            }

            var ok = authState.IsAuthenticated(page.Url, html, 200)
                     && !loginDetector.LooksLikeLoginPage(page.Url, html, null);
            var cookies = await context.CookiesAsync();
            var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
            await context.CloseAsync();
            await browser.CloseAsync();
            playwright.Dispose();
            return (ok, ManualTakeoverReason.None, ok ? cookieHeader : null, false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Playwright login unavailable or failed for run {RunId}", runId);
            try { if (context is not null) await context.CloseAsync(); } catch { /* ignore */ }
            try { if (browser is not null) await browser.CloseAsync(); } catch { /* ignore */ }
            try { playwright?.Dispose(); } catch { /* ignore */ }
            return (false, ManualTakeoverReason.UserRequested, null, false);
        }
    }

    private static string? ResolveLoginUrl(Uri baseUri, string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            return new Uri(baseUri, "/login").ToString();
        }

        var raw = requested.Trim();
        if (!raw.Contains("://", StringComparison.Ordinal))
        {
            return new Uri(baseUri, raw.StartsWith('/') ? raw : "/" + raw).ToString();
        }

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var absolute)
            || (absolute.Scheme != Uri.UriSchemeHttps && absolute.Scheme != Uri.UriSchemeHttp))
        {
            return null;
        }

        var host = absolute.Host.TrimStart('.').ToLowerInvariant();
        var target = baseUri.Host.TrimStart('.').ToLowerInvariant();
        return host == target || host.EndsWith("." + target, StringComparison.Ordinal)
            ? absolute.ToString()
            : null;
    }

    private async Task<(string? Html, int Status)> SafeGetAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return (body, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or SocketException or InvalidOperationException)
        {
            logger.LogDebug(ex, "Login discovery probe failed for {Url}", url);
            return (null, 0);
        }
    }

    /// <summary>
    /// Kullanıcının girişi kendi yapacağı görünür tarayıcıyı açar ve oturumu açık tutar.
    /// Navigasyon başarısız olsa bile pencere elde tutulur; kullanıcı elle gezinebilir.
    /// </summary>
    private async Task<bool> OpenHeldBrowserAsync(Guid runId, string url, CancellationToken cancellationToken)
    {
        if (!options.Value.EnablePlaywright)
        {
            return false;
        }

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        IBrowserContext? context = null;
        try
        {
            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
            context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = env.IsDevelopment()
            });
            var page = await context.NewPageAsync();
            HoldBrowser(runId, playwright, browser, context, page);

            try
            {
                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 45_000
                });
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Manual login navigation failed for {Url}; window kept open", url);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Headed browser launch failed for run {RunId}", runId);
            if (context is not null)
            {
                try { await context.CloseAsync(); } catch { /* ignore */ }
            }

            if (browser is not null)
            {
                try { await browser.CloseAsync(); } catch { /* ignore */ }
            }

            playwright?.Dispose();
            return false;
        }
    }

    private void HoldBrowser(Guid runId, IPlaywright playwright, IBrowser browser, IBrowserContext context, IPage page)
    {
        browserHold.Hold(runId, new HeldBrowserSession
        {
            RunId = runId,
            Playwright = playwright,
            Browser = browser,
            Context = context,
            Page = page
        });
    }

    private HttpClient CreateClient()
    {
        var client = httpFactory.Create(TimeSpan.FromSeconds(15), maxRedirects: 5, allowRedirects: true);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Kaan.SecurityPlatform-AuthenticatedScan/1.0 (+safe-research; owned-test-account)");
        return client;
    }

    internal static AuthScanComparisonResult Compare(ScanModeObservation anon, ScanModeObservation auth)
    {
        if (anon.LoginDetected || anon.AccessDeniedDetected || anon.StatusCode is 401 or 403)
        {
            if (auth.AuthenticationConfirmed || (auth.StatusCode is >= 200 and < 300 && !auth.LoginDetected))
            {
                return AuthScanComparisonResult.LoginRequired;
            }

            if (auth.AccessDeniedDetected || auth.StatusCode is 401 or 403)
            {
                return AuthScanComparisonResult.AccessDeniedAsExpected;
            }
        }

        if (anon.StatusCode is >= 200 and < 300 && auth.StatusCode is >= 200 and < 300
            && string.Equals(anon.ResponseHash, auth.ResponseHash, StringComparison.Ordinal))
        {
            return AuthScanComparisonResult.PublicInBothModes;
        }

        if (anon.StatusCode is 401 or 403 or 404
            && auth.StatusCode is >= 200 and < 300
            && auth.AuthenticationConfirmed)
        {
            // Authenticated-only visibility is NOT automatically a vulnerability.
            return AuthScanComparisonResult.AuthenticatedOnly;
        }

        if (!string.Equals(anon.ResponseHash, auth.ResponseHash, StringComparison.Ordinal))
        {
            return AuthScanComparisonResult.DifferentContentAfterLogin;
        }

        return AuthScanComparisonResult.Inconclusive;
    }

    private async Task<AuthenticatedScanRunDto?> MapAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await db.AuthenticatedScanRuns.AsNoTracking()
            .Include(r => r.Observations)
            .Include(r => r.TestAccount)
            .FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);
        if (run is null) return null;

        var anon = run.Observations.Where(o => !o.IsAuthenticatedMode).Select(MapObs).ToList();
        var auth = run.Observations.Where(o => o.IsAuthenticatedMode).Select(MapObs).ToList();
        var comparisons = run.Observations.Where(o => o.IsAuthenticatedMode && o.ComparisonResult is not null)
            .Select(MapObs).ToList();

        return new AuthenticatedScanRunDto(
            run.Id, run.TargetId, run.TestAccountId,
            run.TestAccount is null ? null : MaskLabel(run.TestAccount.Label),
            run.Status, run.TakeoverReason, run.TakeoverMessage,
            run.StartedAt, run.CompletedAt, run.MaxRequestCount, run.ActualRequestCount,
            run.StopReason, run.AuthenticationConfirmed, run.LoginUrlUsed,
            browserHold.IsHeld(run.Id), run.ErrorCode, run.ErrorMessage,
            anon, auth, comparisons);
    }

    private static ScanModeObservationDto MapObs(ScanModeObservation o) =>
        new(o.IsAuthenticatedMode, o.MaskedAccountLabel, o.Url, o.StatusCode, o.FinalUrl, o.RedirectChain,
            o.ContentType, o.ResponseHash, o.LoginDetected, o.AccessDeniedDetected, o.AuthenticationConfirmed,
            o.RedactedEvidence, o.ComparisonResult);

    private static string MaskLabel(string label) =>
        string.IsNullOrWhiteSpace(label) ? "Security Test Account" : label.Trim();
}
