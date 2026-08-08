using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Application.Features.Scans;
using Kaan.SecurityPlatform.Application.Features.Scans.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kaan.SecurityPlatform.Api.Controllers;

[ApiController]
[Route("api/scans")]
[Authorize(Policy = PolicyNames.RequireApprovedMember)]
public sealed class ScansController : ControllerBase
{
    private readonly IScanService _scans;

    public ScansController(IScanService scans)
    {
        _scans = scans;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? projectId, CancellationToken cancellationToken)
        => Ok(await _scans.ListAsync(projectId, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _scans.GetAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpGet("{id:guid}/progress")]
    public async Task<IActionResult> Progress(Guid id, CancellationToken cancellationToken)
    {
        var result = await _scans.GetProgressAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.CanStartScan)]
    [EnableRateLimiting("scan-start")]
    public async Task<IActionResult> Start([FromBody] StartScanRequest request, CancellationToken cancellationToken)
    {
        var result = await _scans.StartAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: result.ErrorCode, detail: result.ErrorMessage);
        }
        return AcceptedAtAction(nameof(Get), new { id = result.Value!.ScanJobId }, result.Value);
    }

    [HttpPost("retest")]
    [Authorize(Policy = PolicyNames.CanStartScan)]
    [EnableRateLimiting("scan-start")]
    public async Task<IActionResult> Retest([FromBody] RetestRequest request, CancellationToken cancellationToken)
    {
        var result = await _scans.RetestFindingAsync(request, cancellationToken);
        return result.IsSuccess ? Accepted(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }
}
