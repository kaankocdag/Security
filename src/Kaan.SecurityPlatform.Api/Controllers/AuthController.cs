using System.Security.Claims;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Features.Auth;
using Kaan.SecurityPlatform.Application.Features.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaan.SecurityPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _auth;
    private readonly ICurrentUser _currentUser;

    public AuthController(IAuthenticationService auth, ICurrentUser currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var result = await _auth.RegisterAsync(request, ip, ua, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ValidationErrors is not null)
            {
                return ValidationProblem(new ValidationProblemDetails(result.ValidationErrors.ToDictionary(k => k.Key, v => v.Value)));
            }
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: result.ErrorCode, detail: result.ErrorMessage);
        }
        return CreatedAtAction(nameof(Me), null, result.Value);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var result = await _auth.LoginAsync(request, ip, ua, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: result.ErrorCode, detail: result.ErrorMessage);
        }
        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _auth.RefreshAsync(request, ip, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: result.ErrorCode, detail: result.ErrorMessage);
        }
        return Ok(result.Value);
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest request, CancellationToken cancellationToken)
    {
        await _auth.RevokeAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
        {
            return Unauthorized();
        }
        var user = await _auth.GetCurrentUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }
        return Ok(user);
    }
}
