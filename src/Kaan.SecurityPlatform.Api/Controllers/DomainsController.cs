using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Application.Features.Domains;
using Kaan.SecurityPlatform.Application.Features.Domains.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaan.SecurityPlatform.Api.Controllers;

[ApiController]
[Route("api/domains")]
[Authorize(Policy = PolicyNames.RequireApprovedMember)]
public sealed class DomainsController : ControllerBase
{
    private readonly IDomainAssetService _service;

    public DomainsController(IDomainAssetService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? projectId, CancellationToken cancellationToken)
        => Ok(await _service.ListAsync(projectId, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDomainRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: result.ErrorCode, detail: result.ErrorMessage);
        }
        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPost("{id:guid}/verification/start")]
    public async Task<IActionResult> StartVerification(Guid id, [FromBody] StartVerificationRequestBody body, CancellationToken cancellationToken)
    {
        var result = await _service.StartVerificationAsync(new StartVerificationRequest(id, body.Method), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("{id:guid}/verification/run")]
    public async Task<IActionResult> RunVerification(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.RunVerificationAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    /// <summary>SystemAdmin: DNS/HTML kontrolü olmadan doğrulama durumunu manuel ayarlar.</summary>
    [HttpPost("{id:guid}/verification/manual")]
    [Authorize(Policy = PolicyNames.RequireSystemAdmin)]
    public async Task<IActionResult> SetVerificationManual(
        Guid id,
        [FromBody] SetVerificationManualBody body,
        CancellationToken cancellationToken)
    {
        var result = await _service.SetVerificationManualAsync(
            new SetVerificationManualRequest(id, body.IsVerified, body.Note),
            cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.ArchiveAsync(id, cancellationToken);
        return result.IsSuccess ? NoContent() : NotFound();
    }

    public sealed record StartVerificationRequestBody(Domain.Enums.VerificationMethod Method);

    public sealed record SetVerificationManualBody(bool IsVerified, string? Note = null);
}
