using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kaan.SecurityPlatform.Infrastructure.HackerOne;

public sealed class HttpHackerOneApiClient : IHackerOneApiClient
{
    /// <summary>HackerOne structured_scopes rate limit is 50 req/min; keep a buffer.</summary>
    private static readonly TimeSpan ScopeRequestSpacing = TimeSpan.FromMilliseconds(1300);
    private static readonly SemaphoreSlim ScopeGate = new(1, 1);
    private static DateTime _lastScopeRequestUtc = DateTime.MinValue;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApplicationDbContext _db;
    private readonly IHackerOneSecretProtector _protector;
    private readonly HackerOneOptions _options;
    private readonly ILogger<HttpHackerOneApiClient> _logger;

    public HttpHackerOneApiClient(
        IHttpClientFactory httpClientFactory,
        IApplicationDbContext db,
        IHackerOneSecretProtector protector,
        IOptions<HackerOneOptions> options,
        ILogger<HttpHackerOneApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _db = db;
        _protector = protector;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsEnabled => _options.ApiEnabled;

    public async Task<Result<IReadOnlyList<HackerOneRemoteProgram>>> ListProgramsAsync(CancellationToken cancellationToken = default)
    {
        var auth = await TryCreateAuthHeaderAsync(cancellationToken);
        if (auth.IsFailure)
        {
            return Result<IReadOnlyList<HackerOneRemoteProgram>>.Failure(auth.ErrorCode!, auth.ErrorMessage!);
        }

        try
        {
            var list = new List<HackerOneRemoteProgram>();
            var page = 1;
            const int pageSize = 100;

            while (true)
            {
                var path = $"/hackers/programs?page[number]={page}&page[size]={pageSize}";
                var bodyResult = await SendGetAsync(auth.Value!, path, cancellationToken);
                if (bodyResult.IsFailure)
                {
                    return Result<IReadOnlyList<HackerOneRemoteProgram>>.Failure(bodyResult.ErrorCode!, bodyResult.ErrorMessage!);
                }

                using var doc = JsonDocument.Parse(bodyResult.Value!);
                var batch = 0;
                if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        var parsed = ParseProgram(item);
                        if (parsed is not null)
                        {
                            list.Add(parsed);
                            batch++;
                        }
                    }
                }

                if (batch == 0 || !HasNextPage(doc.RootElement, page, pageSize, batch))
                {
                    break;
                }

                page++;
            }

            return Result<IReadOnlyList<HackerOneRemoteProgram>>.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HackerOne ListPrograms failed");
            return Result<IReadOnlyList<HackerOneRemoteProgram>>.Failure("hackerone_api_error", "HackerOne API çağrısı başarısız.");
        }
    }

    public async Task<Result<IReadOnlyList<HackerOneRemoteScope>>> ListStructuredScopesAsync(
        string programHandle,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(programHandle))
        {
            return Result<IReadOnlyList<HackerOneRemoteScope>>.Failure("invalid_handle", "Program handle gerekli.");
        }

        var auth = await TryCreateAuthHeaderAsync(cancellationToken);
        if (auth.IsFailure)
        {
            return Result<IReadOnlyList<HackerOneRemoteScope>>.Failure(auth.ErrorCode!, auth.ErrorMessage!);
        }

        try
        {
            var list = new List<HackerOneRemoteScope>();
            var page = 1;
            const int pageSize = 100;
            var handle = programHandle.Trim();

            while (true)
            {
                await WaitForScopeRateLimitAsync(cancellationToken);
                var path =
                    $"/hackers/programs/{Uri.EscapeDataString(handle)}/structured_scopes?page[number]={page}&page[size]={pageSize}";
                var bodyResult = await SendGetAsync(auth.Value!, path, cancellationToken);
                if (bodyResult.IsFailure)
                {
                    return Result<IReadOnlyList<HackerOneRemoteScope>>.Failure(bodyResult.ErrorCode!, bodyResult.ErrorMessage!);
                }

                using var doc = JsonDocument.Parse(bodyResult.Value!);
                var batch = 0;
                if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        var parsed = ParseScope(item);
                        if (parsed is not null)
                        {
                            list.Add(parsed);
                            batch++;
                        }
                    }
                }

                if (batch == 0 || !HasNextPage(doc.RootElement, page, pageSize, batch))
                {
                    break;
                }

                page++;
            }

            return Result<IReadOnlyList<HackerOneRemoteScope>>.Success(list);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HackerOne ListStructuredScopes failed for {Handle}", programHandle);
            return Result<IReadOnlyList<HackerOneRemoteScope>>.Failure(
                "hackerone_api_error",
                $"Structured scopes alınamadı ({programHandle}).");
        }
    }

    public async Task<Result<HackerOneRemoteSubmission>> SubmitReportAsync(
        HackerOneSubmitPayload payload,
        CancellationToken cancellationToken = default)
    {
        var auth = await TryCreateAuthHeaderAsync(cancellationToken);
        if (auth.IsFailure)
        {
            return Result<HackerOneRemoteSubmission>.Failure(auth.ErrorCode!, auth.ErrorMessage!);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("HackerOne");
            var json = JsonSerializer.Serialize(new
            {
                data = new
                {
                    type = "report",
                    attributes = new
                    {
                        title = payload.Title,
                        vulnerability_information = payload.MarkdownBody,
                        impact = payload.Severity
                    }
                }
            });

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.BaseUrl.TrimEnd('/')}/hackers/programs/{Uri.EscapeDataString(payload.ProgramHandle)}/reports")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = auth.Value;
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("HackerOne submit failed: {Status} {Body}", (int)response.StatusCode, Truncate(body));
                return Result<HackerOneRemoteSubmission>.Failure(
                    "hackerone_submit_failed",
                    $"HackerOne gönderimi başarısız ({(int)response.StatusCode}).");
            }

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            var externalId = doc.RootElement.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idEl)
                ? idEl.GetString() ?? Guid.NewGuid().ToString("N")
                : Guid.NewGuid().ToString("N");
            var url = $"https://hackerone.com/reports/{externalId}";
            return Result<HackerOneRemoteSubmission>.Success(new HackerOneRemoteSubmission(externalId, url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HackerOne SubmitReport failed");
            return Result<HackerOneRemoteSubmission>.Failure("hackerone_submit_failed", "HackerOne gönderimi sırasında hata oluştu.");
        }
    }

    private async Task<Result<string>> SendGetAsync(
        AuthenticationHeaderValue auth,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("HackerOne");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_options.BaseUrl.TrimEnd('/')}{relativePath}");
        request.Headers.Authorization = auth;
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("HackerOne GET {Path} failed: {Status} {Body}", relativePath, (int)response.StatusCode, Truncate(body));
            if ((int)response.StatusCode == 401)
            {
                return Result<string>.Failure(
                    "hackerone_unauthorized",
                    "HackerOne 401: kullanıcı adı veya token hatalı. Settings → HackerOne’da " +
                    "HackerOne username (handle) + API token değerini yeniden kaydedin. " +
                    "E-posta kullanma; kişisel token’da ayrı ‘identifier adı’ yoktur — kullanıcı adın yeter.");
            }

            return Result<string>.Failure(
                "hackerone_api_error",
                $"HackerOne isteği başarısız ({(int)response.StatusCode}).");
        }

        return Result<string>.Success(body);
    }

    private static async Task WaitForScopeRateLimitAsync(CancellationToken cancellationToken)
    {
        await ScopeGate.WaitAsync(cancellationToken);
        try
        {
            var elapsed = DateTime.UtcNow - _lastScopeRequestUtc;
            if (elapsed < ScopeRequestSpacing)
            {
                await Task.Delay(ScopeRequestSpacing - elapsed, cancellationToken);
            }

            _lastScopeRequestUtc = DateTime.UtcNow;
        }
        finally
        {
            ScopeGate.Release();
        }
    }

    private static HackerOneRemoteProgram? ParseProgram(JsonElement item)
    {
        var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
        var attrs = item.TryGetProperty("attributes", out var a) ? a : default;
        if (attrs.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var handle = attrs.TryGetProperty("handle", out var h) ? h.GetString() ?? id : id;
        if (string.IsNullOrWhiteSpace(handle))
        {
            return null;
        }

        var name = attrs.TryGetProperty("name", out var n) ? n.GetString() ?? handle : handle;
        var offers = attrs.TryGetProperty("offers_bounties", out var ob) && ob.ValueKind is JsonValueKind.True or JsonValueKind.False
            && ob.GetBoolean();
        var currency = attrs.TryGetProperty("currency", out var cur) ? cur.GetString() : null;
        var submissionState = attrs.TryGetProperty("submission_state", out var ss) ? ss.GetString() : null;
        var openScope = attrs.TryGetProperty("open_scope", out var os) && os.ValueKind is JsonValueKind.True or JsonValueKind.False
            && os.GetBoolean();
        var state = attrs.TryGetProperty("state", out var st) ? st.GetString() : null;

        return new HackerOneRemoteProgram(id, handle, name, offers, currency, submissionState, openScope, state);
    }

    private static HackerOneRemoteScope? ParseScope(JsonElement item)
    {
        var id = item.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
        var attrs = item.TryGetProperty("attributes", out var a) ? a : default;
        if (attrs.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var identifier = attrs.TryGetProperty("asset_identifier", out var ai) ? ai.GetString() : null;
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        var assetType = attrs.TryGetProperty("asset_type", out var at) ? at.GetString() ?? "UNKNOWN" : "UNKNOWN";
        var eligibleBounty = attrs.TryGetProperty("eligible_for_bounty", out var eb)
            && eb.ValueKind is JsonValueKind.True or JsonValueKind.False
            && eb.GetBoolean();
        var eligibleSubmission = attrs.TryGetProperty("eligible_for_submission", out var es)
            && es.ValueKind is JsonValueKind.True or JsonValueKind.False
            && es.GetBoolean();
        var maxSeverity = attrs.TryGetProperty("max_severity", out var ms) ? ms.GetString() : null;
        var instruction = attrs.TryGetProperty("instruction", out var ins) ? Truncate(ins.GetString(), 400) : null;

        return new HackerOneRemoteScope(
            id,
            identifier,
            assetType,
            eligibleBounty,
            eligibleSubmission,
            maxSeverity,
            instruction);
    }

    private static bool HasNextPage(JsonElement root, int page, int pageSize, int batchCount)
    {
        if (root.TryGetProperty("links", out var links)
            && links.ValueKind == JsonValueKind.Object
            && links.TryGetProperty("next", out var next)
            && next.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(next.GetString()))
        {
            return true;
        }

        return batchCount >= pageSize;
    }

    private async Task<Result<AuthenticationHeaderValue>> TryCreateAuthHeaderAsync(CancellationToken cancellationToken)
    {
        if (!_options.ApiEnabled)
        {
            return Result<AuthenticationHeaderValue>.Failure(
                "hackerone_api_disabled",
                "HackerOne API kapalı (HackerOne:ApiEnabled=false).");
        }

        var cred = await _db.HackerOneApiCredentials.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Identifier == "default", cancellationToken);
        if (cred is null || string.IsNullOrWhiteSpace(cred.ProtectedApiToken))
        {
            return Result<AuthenticationHeaderValue>.Failure(
                "hackerone_token_missing",
                "HackerOne API token tanımlı değil. Settings üzerinden ekleyin.");
        }

        try
        {
            if (string.IsNullOrWhiteSpace(cred.ApiUsername))
            {
                return Result<AuthenticationHeaderValue>.Failure(
                    "hackerone_token_identifier_missing",
                    "API token identifier (Basic Auth kullanıcı adı) tanımlı değil. Settings'te kaydedin.");
            }

            var token = _protector.Unprotect(cred.ProtectedApiToken);
            var raw = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{cred.ApiUsername}:{token}"));
            return Result<AuthenticationHeaderValue>.Success(new AuthenticationHeaderValue("Basic", raw));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unprotect HackerOne token");
            return Result<AuthenticationHeaderValue>.Failure(
                "hackerone_token_invalid",
                "Saklanan HackerOne token çözülemedi. Token'ı yeniden kaydedin.");
        }
    }

    private static string Truncate(string? value, int max = 400) =>
        string.IsNullOrEmpty(value) ? "" : value.Length <= max ? value : value[..max];
}

/// <summary>ApiEnabled + token varsa Http, aksi halde Null istemciye delege eder.</summary>
public sealed class FeatureFlagHackerOneApiClient : IHackerOneApiClient
{
    private readonly HackerOneOptions _options;
    private readonly NullHackerOneApiClient _nullClient;
    private readonly HttpHackerOneApiClient _httpClient;

    public FeatureFlagHackerOneApiClient(
        IOptions<HackerOneOptions> options,
        NullHackerOneApiClient nullClient,
        HttpHackerOneApiClient httpClient)
    {
        _options = options.Value;
        _nullClient = nullClient;
        _httpClient = httpClient;
    }

    public bool IsEnabled => _options.ApiEnabled;

    private IHackerOneApiClient Active => _options.ApiEnabled ? _httpClient : _nullClient;

    public Task<Result<IReadOnlyList<HackerOneRemoteProgram>>> ListProgramsAsync(CancellationToken cancellationToken = default)
        => Active.ListProgramsAsync(cancellationToken);

    public Task<Result<IReadOnlyList<HackerOneRemoteScope>>> ListStructuredScopesAsync(
        string programHandle,
        CancellationToken cancellationToken = default)
        => Active.ListStructuredScopesAsync(programHandle, cancellationToken);

    public Task<Result<HackerOneRemoteSubmission>> SubmitReportAsync(HackerOneSubmitPayload payload, CancellationToken cancellationToken = default)
        => Active.SubmitReportAsync(payload, cancellationToken);
}
