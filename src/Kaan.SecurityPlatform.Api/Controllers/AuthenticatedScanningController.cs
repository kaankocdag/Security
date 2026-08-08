using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning;
using Kaan.SecurityPlatform.Application.Features.AuthenticatedScanning.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaan.SecurityPlatform.Api.Controllers;

[ApiController]
[Route("api/authenticated-scanning")]
[Authorize(Policy = PolicyNames.CanManageBugBounty)]
public sealed class AuthenticatedScanningController(
    IAuthenticatedScanOrchestrator orchestrator,
    ITestAccountManagementService accounts) : ControllerBase
{
    [HttpGet("targets/{targetId:guid}/preconditions")]
    [Authorize(Policy = PolicyNames.CanRunAuthenticatedScan)]
    public async Task<IActionResult> Preconditions(Guid targetId, CancellationToken cancellationToken)
    {
        var result = await orchestrator.GetPreconditionsAsync(targetId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpGet("targets/{targetId:guid}/accounts")]
    [Authorize(Policy = PolicyNames.CanManageTestAccounts)]
    public async Task<IActionResult> ListAccounts(Guid targetId, CancellationToken cancellationToken)
        => Ok(await accounts.ListAsync(targetId, cancellationToken));

    [HttpPost("accounts/register-existing")]
    [Authorize(Policy = PolicyNames.CanManageTestAccounts)]
    public async Task<IActionResult> RegisterExisting(
        [FromBody] RegisterExistingTestAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accounts.RegisterExistingAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("identity-profiles")]
    [Authorize(Policy = PolicyNames.CanManageTestAccounts)]
    public async Task<IActionResult> CreateIdentity(
        [FromBody] UpsertTestIdentityProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accounts.CreateIdentityProfileAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(new { id = result.Value }) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("targets/{targetId:guid}/registration-plan/{identityProfileId:guid}")]
    [Authorize(Policy = PolicyNames.CanApproveRegistration)]
    public async Task<IActionResult> PlanRegistration(
        Guid targetId,
        Guid identityProfileId,
        CancellationToken cancellationToken)
    {
        var result = await accounts.PlanRegistrationAsync(targetId, identityProfileId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("registration/confirm-submit")]
    [Authorize(Policy = PolicyNames.CanApproveRegistration)]
    public async Task<IActionResult> ConfirmSubmit(
        [FromBody] ConfirmRegistrationSubmitRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accounts.ConfirmRegistrationSubmitAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("accounts/{accountId:guid}/reveal-password")]
    [Authorize(Policy = PolicyNames.CanRevealTestAccountSecret)]
    public async Task<IActionResult> RevealPassword(
        Guid accountId,
        [FromQuery] bool forCopy = false,
        CancellationToken cancellationToken = default)
    {
        var result = await accounts.RevealPasswordAsync(accountId, forCopy, cancellationToken);
        return result.IsSuccess ? Ok(new { password = result.Value }) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("accounts/{accountId:guid}/change-password")]
    [Authorize(Policy = PolicyNames.CanManageTestAccounts)]
    public async Task<IActionResult> ChangePassword(
        Guid accountId,
        [FromBody] ChangePasswordBody body,
        CancellationToken cancellationToken)
    {
        var result = await accounts.ChangePasswordAsync(accountId, body.NewPassword, cancellationToken);
        return result.IsSuccess ? Ok() : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("accounts/{accountId:guid}/disable")]
    [Authorize(Policy = PolicyNames.CanManageTestAccounts)]
    public async Task<IActionResult> Disable(Guid accountId, CancellationToken cancellationToken)
    {
        var result = await accounts.DisableAsync(accountId, cancellationToken);
        return result.IsSuccess ? Ok() : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpDelete("accounts/{accountId:guid}/vault")]
    [Authorize(Policy = PolicyNames.CanDeleteTestAccount)]
    public async Task<IActionResult> DeleteVault(Guid accountId, CancellationToken cancellationToken)
    {
        var result = await accounts.DeleteVaultAsync(accountId, cancellationToken);
        return result.IsSuccess ? Ok() : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("runs/start")]
    [Authorize(Policy = PolicyNames.CanRunAuthenticatedScan)]
    public async Task<IActionResult> Start(
        [FromBody] StartAuthenticatedScanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await orchestrator.StartAuthenticatedScanAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("runs/start-manual-login")]
    [Authorize(Policy = PolicyNames.CanRunAuthenticatedScan)]
    public async Task<IActionResult> StartManualLogin(
        [FromBody] StartManualLoginSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await orchestrator.StartManualLoginSessionAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("runs/start-cookie-session")]
    [Authorize(Policy = PolicyNames.CanRunAuthenticatedScan)]
    public async Task<IActionResult> StartCookieSession(
        [FromBody] StartCookieSessionScanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await orchestrator.StartCookieSessionScanAsync(request, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpGet("targets/{targetId:guid}/login-discovery")]
    [Authorize(Policy = PolicyNames.CanRunAuthenticatedScan)]
    public async Task<IActionResult> DiscoverLogin(Guid targetId, CancellationToken cancellationToken)
    {
        var result = await orchestrator.DiscoverLoginAsync(targetId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpGet("runs/{runId:guid}")]
    [Authorize(Policy = PolicyNames.CanRunAuthenticatedScan)]
    public async Task<IActionResult> GetRun(Guid runId, CancellationToken cancellationToken)
    {
        var result = await orchestrator.GetRunAsync(runId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    [HttpPost("runs/{runId:guid}/stop")]
    [Authorize(Policy = PolicyNames.CanRunAuthenticatedScan)]
    public async Task<IActionResult> Stop(Guid runId, CancellationToken cancellationToken)
    {
        var result = await orchestrator.StopAsync(runId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    [HttpPost("runs/{runId:guid}/continue-after-takeover")]
    [Authorize(Policy = PolicyNames.CanRunAuthenticatedScan)]
    public async Task<IActionResult> ContinueAfterTakeover(Guid runId, CancellationToken cancellationToken)
    {
        var result = await orchestrator.ContinueAfterManualTakeoverAsync(runId, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(title: result.ErrorCode, detail: result.ErrorMessage);
    }

    public sealed record ChangePasswordBody(string NewPassword);
}
