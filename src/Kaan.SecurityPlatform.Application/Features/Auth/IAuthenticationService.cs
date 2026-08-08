using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Auth.Dtos;

namespace Kaan.SecurityPlatform.Application.Features.Auth;

public interface IAuthenticationService
{
    Task<Result<RegisterResponse>> RegisterAsync(RegisterRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken = default);
    Task<Result> RevokeAsync(RevokeRequest request, CancellationToken cancellationToken = default);
    Task<CurrentUserDto?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
