using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Application.Features.Validation;
using Kaan.SecurityPlatform.Application.Features.Validation.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaan.SecurityPlatform.Api.Controllers;

[ApiController]
[Route("api/findings/validation")]
[Authorize(Policy = PolicyNames.RequireApprovedMember)]
public sealed class FindingValidationController : ControllerBase
{
    private readonly IFindingValidationOrchestrator _orchestrator;
    private readonly IAuthorizationEvidenceService _authEvidence;
    private readonly IValidationCatalogService _catalog;

    public FindingValidationController(
        IFindingValidationOrchestrator orchestrator,
        IAuthorizationEvidenceService authEvidence,
        IValidationCatalogService catalog)
    {
        _orchestrator = orchestrator;
        _authEvidence = authEvidence;
        _catalog = catalog;
    }

    [HttpGet("{findingId:guid}/preconditions")]
    public async Task<IActionResult> Preconditions(Guid findingId, CancellationToken cancellationToken)
    {
        var result = await _orchestrator.GetPreconditionsAsync(findingId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartFindingValidationRequest request, CancellationToken cancellationToken)
    {
        var result = await _orchestrator.StartAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpGet("runs/{runId:guid}")]
    public async Task<IActionResult> GetRun(Guid runId, CancellationToken cancellationToken)
    {
        var result = await _orchestrator.GetRunAsync(runId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost("runs/{runId:guid}/stop")]
    public async Task<IActionResult> Stop(Guid runId, CancellationToken cancellationToken)
    {
        var result = await _orchestrator.StopAsync(runId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpGet("by-finding/{findingId:guid}")]
    public async Task<IActionResult> ListByFinding(Guid findingId, CancellationToken cancellationToken)
        => Ok(await _orchestrator.ListRunsForFindingAsync(findingId, cancellationToken));

    [HttpPut("authorization-evidence")]
    public async Task<IActionResult> UpsertAuth([FromBody] UpsertAuthorizationEvidenceRequest request, CancellationToken cancellationToken)
    {
        var result = await _authEvidence.UpsertAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPut("scope-policy")]
    public async Task<IActionResult> UpsertScope([FromBody] UpsertScopePolicyRequest request, CancellationToken cancellationToken)
    {
        var result = await _catalog.UpsertScopeAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(new { result.Value!.Id, result.Value.TargetId, result.Value.ScopeStatus })
            : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPut("test-accounts")]
    public async Task<IActionResult> UpsertTestAccount([FromBody] UpsertTestAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await _catalog.UpsertTestAccountAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(new { id = result.Value }) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }
}
