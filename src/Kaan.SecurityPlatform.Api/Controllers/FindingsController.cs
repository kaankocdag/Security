using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Application.Features.Findings;
using Kaan.SecurityPlatform.Application.Features.Findings.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaan.SecurityPlatform.Api.Controllers;

[ApiController]
[Route("api/findings")]
[Authorize(Policy = PolicyNames.RequireApprovedMember)]
public sealed class FindingsController : ControllerBase
{
    private readonly IFindingService _findings;

    public FindingsController(IFindingService findings)
    {
        _findings = findings;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? scanResultId,
        [FromQuery] Guid? projectId,
        CancellationToken cancellationToken)
        => Ok(await _findings.ListAsync(scanResultId, projectId, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _findings.GetAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateFindingStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _findings.UpdateStatusAsync(id, request, cancellationToken);
        return result.IsSuccess ? NoContent() : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }
}
