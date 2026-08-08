using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Application.Features.Admin;
using Kaan.SecurityPlatform.Application.Features.Admin.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaan.SecurityPlatform.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = PolicyNames.CanApproveMembership)]
public sealed class AdminMembershipController : ControllerBase
{
    private readonly IMembershipApprovalService _service;

    public AdminMembershipController(IMembershipApprovalService service)
    {
        _service = service;
    }

    [HttpGet("users/pending")]
    public async Task<IActionResult> ListPendingUsers(CancellationToken cancellationToken)
        => Ok(await _service.ListPendingUsersAsync(cancellationToken));

    [HttpGet("companies/pending")]
    public async Task<IActionResult> ListPendingCompanies(CancellationToken cancellationToken)
        => Ok(await _service.ListPendingCompaniesAsync(cancellationToken));

    [HttpPost("users/{id:guid}/approve")]
    public async Task<IActionResult> ApproveUser(Guid id, [FromBody] ApproveUserRequestBody? body, CancellationToken cancellationToken)
    {
        var result = await _service.ApproveUserAsync(new ApproveUserRequest(id, body?.Note), cancellationToken);
        return result.IsSuccess ? Ok() : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("users/{id:guid}/reject")]
    public async Task<IActionResult> RejectUser(Guid id, [FromBody] RejectUserRequestBody body, CancellationToken cancellationToken)
    {
        var result = await _service.RejectUserAsync(new RejectUserRequest(id, body.Reason), cancellationToken);
        return result.IsSuccess ? Ok() : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("users/{id:guid}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid id, [FromBody] RejectUserRequestBody body, CancellationToken cancellationToken)
    {
        var result = await _service.SuspendUserAsync(new SuspendUserRequest(id, body.Reason), cancellationToken);
        return result.IsSuccess ? Ok() : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("companies/{id:guid}/approve")]
    public async Task<IActionResult> ApproveCompany(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.ApproveCompanyAsync(new ApproveCompanyRequest(id), cancellationToken);
        return result.IsSuccess ? Ok() : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("companies/{id:guid}/suspend")]
    public async Task<IActionResult> SuspendCompany(Guid id, [FromBody] RejectUserRequestBody body, CancellationToken cancellationToken)
    {
        var result = await _service.SuspendCompanyAsync(new SuspendCompanyRequest(id, body.Reason), cancellationToken);
        return result.IsSuccess ? Ok() : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    public sealed record ApproveUserRequestBody(string? Note);
    public sealed record RejectUserRequestBody(string Reason);
}
