namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

public interface IJwtTokenService
{
    Task<AuthenticationTokens> IssueAsync(
        Guid userId,
        string email,
        IEnumerable<string> roles,
        IReadOnlyDictionary<string, string> additionalClaims,
        CancellationToken cancellationToken = default);

    Task<RefreshOutcome?> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}

public sealed record AuthenticationTokens(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    string TokenType = "Bearer");

public sealed record RefreshOutcome(Guid UserId, AuthenticationTokens Tokens);
