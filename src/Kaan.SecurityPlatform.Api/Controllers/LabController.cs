using System.Text.Json;
using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;
using Kaan.SecurityPlatform.Application.Features.Lab;
using Kaan.SecurityPlatform.Application.Features.Lab.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kaan.SecurityPlatform.Api.Controllers;

/// <summary>
/// IsolatedSecurityLab — imzalı senaryolar; AuthorizedExternalAssessment ayrı /api/scans modudur.
/// </summary>
[ApiController]
[Route("api/admin/lab")]
[Authorize(Policy = PolicyNames.CanManageLab)]
public sealed class LabController : ControllerBase
{
    private readonly ILabExecutionService _lab;
    private readonly ILabStartRequestGuard _guard;

    public LabController(ILabExecutionService lab, ILabStartRequestGuard guard)
    {
        _lab = lab;
        _guard = guard;
    }

    [HttpPost("elevation")]
    [EnableRateLimiting("lab-elevate")]
    public async Task<IActionResult> Elevate([FromBody] ElevateLabRequest request, CancellationToken cancellationToken)
    {
        var result = await _lab.ElevateAsync(request, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpGet("scenarios")]
    public async Task<IActionResult> ListScenarios(CancellationToken cancellationToken)
        => Ok(await _lab.ListScenariosAsync(cancellationToken));

    [HttpGet("targets")]
    public async Task<IActionResult> ListTargets(CancellationToken cancellationToken)
        => Ok(await _lab.ListTargetSitesAsync(cancellationToken));

    [HttpPost("targets")]
    [EnableRateLimiting("lab-start")]
    public async Task<IActionResult> AddTarget([FromBody] CreateLabTargetSiteRequest request, CancellationToken cancellationToken)
    {
        var result = await _lab.AddTargetSiteAsync(request, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost("targets/{id:guid}/disable")]
    public async Task<IActionResult> DisableTarget(Guid id, CancellationToken cancellationToken)
    {
        var result = await _lab.DisableTargetSiteAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok()
            : Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpPost("executions")]
    [EnableRateLimiting("lab-start")]
    public async Task<IActionResult> Start([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var raw = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (body.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in body.EnumerateObject())
            {
                raw[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Null => null,
                    _ => prop.Value.ToString()
                };
            }
        }

        var guard = _guard.ValidateNoForbiddenFields(raw);
        if (guard.IsFailure)
        {
            return Problem(title: guard.ErrorCode, detail: guard.ErrorMessage, statusCode: StatusCodes.Status400BadRequest);
        }

        if (!raw.TryGetValue("labTargetSiteId", out var tidRaw) ||
            !Guid.TryParse(tidRaw?.ToString(), out var labTargetSiteId))
        {
            return Problem(
                title: "target_required",
                detail: "labTargetSiteId zorunludur (allowlist hedefi). Serbest URL/IP kabul edilmez.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var request = new StartLabExecutionRequest(
            raw.TryGetValue("scenarioKey", out var sk) ? sk?.ToString() ?? string.Empty : string.Empty,
            raw.TryGetValue("confirmPhrase", out var cp) ? cp?.ToString() ?? string.Empty : string.Empty,
            raw.TryGetValue("elevationToken", out var et) ? et?.ToString() ?? string.Empty : string.Empty,
            labTargetSiteId,
            raw.TryGetValue("assessmentModeName", out var am) ? am?.ToString() : AssessmentModeNames.IsolatedSecurityLab);

        var result = await _lab.StartAsync(request, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpGet("executions")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
        => Ok(await _lab.ListExecutionsAsync(cancellationToken));

    [HttpGet("executions/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _lab.GetAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: StatusCodes.Status404NotFound);
    }

    [HttpGet("executions/{id:guid}/logs")]
    public async Task<IActionResult> Logs(Guid id, CancellationToken cancellationToken)
    {
        var result = await _lab.GetLogsAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: StatusCodes.Status404NotFound);
    }

    [HttpPost("executions/{id:guid}/cancel")]
    [EnableRateLimiting("lab-start")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelLabBody? body, CancellationToken cancellationToken)
    {
        var result = await _lab.CancelAsync(id, body?.ReasonTr, cancellationToken);
        return result.IsSuccess
            ? Ok()
            : Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: StatusCodes.Status400BadRequest);
    }

    public sealed record CancelLabBody(string? ReasonTr);
}
