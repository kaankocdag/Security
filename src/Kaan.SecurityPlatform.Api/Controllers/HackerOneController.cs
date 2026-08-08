using Hangfire;
using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Kaan.SecurityPlatform.Application.Features.HackerOne.Dtos;
using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaan.SecurityPlatform.Api.Controllers;

[ApiController]
[Route("api/hackerone")]
[Authorize(Policy = PolicyNames.CanManageBugBounty)]
public sealed class HackerOneController : ControllerBase
{
    private readonly IHackerOneWorkspaceService _workspace;

    public HackerOneController(IHackerOneWorkspaceService workspace)
    {
        _workspace = workspace;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<HackerOneOverviewDto>> Overview(CancellationToken cancellationToken)
        => Ok(await _workspace.GetOverviewAsync(cancellationToken));

    [HttpGet("candidates")]
    public async Task<ActionResult<IReadOnlyList<HackerOneCandidateDto>>> Candidates(
        [FromQuery] SubmissionRecommendation? recommendation,
        [FromQuery] string? programPolicyKey,
        CancellationToken cancellationToken)
        => Ok(await _workspace.ListCandidatesAsync(recommendation, programPolicyKey, cancellationToken));

    [HttpGet("programs")]
    public async Task<ActionResult<IReadOnlyList<BugBountyProgramDto>>> Programs(CancellationToken cancellationToken)
        => Ok(await _workspace.ListProgramsAsync(cancellationToken));

    [HttpGet("programs/{id:guid}")]
    public async Task<IActionResult> Program(Guid id, CancellationToken cancellationToken)
    {
        var result = await _workspace.GetProgramAsync(id, cancellationToken);
        return result.IsFailure
            ? Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: 404)
            : Ok(result.Value);
    }

    [HttpPut("programs/{id:guid}/enabled")]
    public async Task<IActionResult> SetProgramEnabled(Guid id, [FromBody] SetProgramEnabledRequest body, CancellationToken cancellationToken)
    {
        var result = await _workspace.UpdateProgramEnabledAsync(id, body.IsEnabled, cancellationToken);
        return result.IsFailure
            ? Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: 400)
            : Ok(result.Value);
    }

    [HttpPost("programs/sync")]
    public async Task<IActionResult> SyncPrograms(CancellationToken cancellationToken)
    {
        var result = await _workspace.SyncProgramsAsync(cancellationToken);
        return result.IsFailure
            ? Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: 400)
            : Ok(new { synced = result.Value });
    }

    /// <summary>
    /// Tüm HackerOne program structured_scopes → Domains (Hangfire arka plan).
    /// Rate limit nedeniyle uzun sürebilir; Domainler sayfasını yenileyerek ilerlemeyi görün.
    /// </summary>
    [HttpPost("domains/sync-scopes")]
    public async Task<IActionResult> EnqueueSyncScopesToDomains(
        [FromServices] IBackgroundJobClient jobs,
        [FromServices] IHackerOneApiClient apiClient,
        CancellationToken cancellationToken)
    {
        // 401 vb. hataları kuyruğa almadan önce yakala — UI hemen görsün.
        var probe = await apiClient.ListProgramsAsync(cancellationToken);
        if (probe.IsFailure)
        {
            return Problem(title: probe.ErrorCode, detail: probe.ErrorMessage, statusCode: 400);
        }

        var jobId = jobs.Enqueue<IHackerOneScopeSyncJob>(x => x.ExecuteAsync(CancellationToken.None));
        return Accepted(new
        {
            jobId,
            message =
                $"HackerOne scope senkronizasyonu kuyruğa alındı ({probe.Value!.Count} program görüldü). " +
                "Domainler arka planda eklenir; API kesin $ tutarı vermez — bounty eligible / currency / max severity özeti yazılır."
        });
    }

    /// <summary>Kullanıcının elle girdiği yetkili bir test hedefini targets listesine ekler (doğrulanmış).</summary>
    [HttpPost("targets/manual")]
    public async Task<IActionResult> AddManualTarget(
        [FromBody] AddManualTargetRequest body,
        CancellationToken cancellationToken)
    {
        var result = await _workspace.AddManualTargetAsync(body.HostName, body.AuthorizedConfirmed, cancellationToken);
        return result.IsFailure
            ? Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: 400)
            : Ok(new { domainAssetId = result.Value });
    }

    /// <summary>Hedef için son ASC özeti — neler tarandı, bulgular (0 olsa da), rapor indirme için scanJobId.</summary>
    [HttpGet("targets/{domainAssetId:guid}/latest-assessment")]
    public async Task<IActionResult> LatestAssessment(Guid domainAssetId, CancellationToken cancellationToken)
    {
        var result = await _workspace.GetLatestAssessmentSummaryAsync(domainAssetId, cancellationToken);
        return result.IsFailure
            ? Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: 404)
            : Ok(result.Value);
    }

    /// <summary>Senkron (test/küçük set). Uzun sürebilir — HTTP timeout riski.</summary>
    [HttpPost("domains/sync-scopes/now")]
    public async Task<IActionResult> SyncScopesToDomainsNow(CancellationToken cancellationToken)
    {
        var result = await _workspace.SyncScopesToDomainsAsync(cancellationToken);
        return result.IsFailure
            ? Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: 400)
            : Ok(result.Value);
    }

    [HttpGet("settings")]
    public async Task<ActionResult<HackerOneWorkspaceSettingsDto>> Settings(CancellationToken cancellationToken)
        => Ok(await _workspace.GetSettingsAsync(cancellationToken));

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateHackerOneWorkspaceSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _workspace.UpdateSettingsAsync(request, cancellationToken);
        return result.IsFailure
            ? Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: 400)
            : Ok(result.Value);
    }

    [HttpPut("settings/api-token")]
    public async Task<IActionResult> SetApiToken([FromBody] SetHackerOneApiTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _workspace.SetApiTokenAsync(request, cancellationToken);
        return result.IsFailure
            ? Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: 400)
            : Ok(new { ok = true });
    }

    [HttpDelete("settings/api-token")]
    public async Task<IActionResult> ClearApiToken(CancellationToken cancellationToken)
    {
        await _workspace.ClearApiTokenAsync(cancellationToken);
        return Ok(new { ok = true });
    }

    [HttpGet("drafts")]
    public async Task<ActionResult<IReadOnlyList<HackerOneReportDraftDto>>> Drafts(CancellationToken cancellationToken)
        => Ok(await _workspace.ListDraftsAsync(cancellationToken));

    [HttpPost("drafts")]
    public async Task<IActionResult> CreateDraft([FromBody] CreateHackerOneDraftRequest request, CancellationToken cancellationToken)
    {
        var result = await _workspace.CreateOrGetDraftAsync(request, cancellationToken);
        return result.IsFailure
            ? Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: 400)
            : Ok(result.Value);
    }

    [HttpGet("drafts/{id:guid}")]
    public async Task<IActionResult> GetDraft(Guid id, CancellationToken cancellationToken)
    {
        var result = await _workspace.GetDraftAsync(id, cancellationToken);
        return result.IsFailure
            ? Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: 404)
            : Ok(result.Value);
    }

    [HttpPut("drafts/{id:guid}")]
    public async Task<IActionResult> UpdateDraft(
        Guid id,
        [FromBody] UpdateHackerOneDraftRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _workspace.UpdateDraftAsync(id, request, cancellationToken);
        return result.IsFailure
            ? Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: 400)
            : Ok(result.Value);
    }

    [HttpGet("drafts/{id:guid}/markdown")]
    public async Task<IActionResult> Markdown(
        Guid id,
        [FromQuery] string? language,
        CancellationToken cancellationToken)
    {
        var result = await _workspace.GetMarkdownAsync(id, language, cancellationToken);
        return result.IsFailure
            ? Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: 404)
            : Ok(result.Value);
    }

    [HttpPost("drafts/{id:guid}/readiness")]
    public async Task<IActionResult> Readiness(Guid id, CancellationToken cancellationToken)
    {
        var result = await _workspace.RecalculateReadinessAsync(id, cancellationToken);
        return result.IsFailure
            ? Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: 404)
            : Ok(result.Value);
    }

    [HttpPost("drafts/{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, [FromBody] SubmitHackerOneDraftRequest request, CancellationToken cancellationToken)
    {
        var result = await _workspace.SubmitDraftAsync(id, request, cancellationToken);
        return result.IsFailure
            ? Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: 400)
            : Ok(result.Value);
    }

    [HttpGet("submissions")]
    public async Task<ActionResult<IReadOnlyList<HackerOneSubmissionDto>>> Submissions(CancellationToken cancellationToken)
        => Ok(await _workspace.ListSubmissionsAsync(cancellationToken));

    [HttpGet("scan-profiles")]
    public async Task<ActionResult<IReadOnlyList<ScanProfileDto>>> ScanProfiles(CancellationToken cancellationToken)
        => Ok(await _workspace.ListScanProfilesAsync(cancellationToken));

    [HttpPost("candidate-assessment")]
    public async Task<IActionResult> StartCandidateAssessment(
        [FromBody] StartCandidateAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _workspace.StartCandidateAssessmentAsync(request, cancellationToken);
        return result.IsFailure
            ? Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: 400)
            : Ok(new { scanJobId = result.Value });
    }

    public sealed record SetProgramEnabledRequest(bool IsEnabled);

    public sealed record AddManualTargetRequest(string HostName, bool AuthorizedConfirmed);
}
