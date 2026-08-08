using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Infrastructure.Identity;
using Kaan.SecurityPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Kaan.SecurityPlatform.Infrastructure.Authentication;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SecurityPlatformDbContext _dbContext;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(
        IOptions<JwtOptions> options,
        SecurityPlatformDbContext dbContext,
        IDateTimeProvider clock,
        ILogger<JwtTokenService> logger)
    {
        _options = options.Value;
        _dbContext = dbContext;
        _clock = clock;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey en az 32 karakter olmalıdır. appsettings veya environment variable üzerinden ayarlayın.");
        }
    }

    public async Task<AuthenticationTokens> IssueAsync(
        Guid userId,
        string email,
        IEnumerable<string> roles,
        IReadOnlyDictionary<string, string> additionalClaims,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var jwtId = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(ClaimTypesExtended.TokenId, jwtId)
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(additionalClaims.Select(c => new Claim(c.Key, c.Value)));

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var accessExpiresAt = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: accessExpiresAt,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
        var refreshTokenRaw = GenerateSecureToken();
        var refreshTokenHash = HashToken(refreshTokenRaw);
        var refreshExpiresAt = now.AddDays(_options.RefreshTokenLifetimeDays);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = refreshTokenHash,
            JwtId = jwtId,
            CreatedAt = now,
            ExpiresAt = refreshExpiresAt
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthenticationTokens(
            accessToken,
            accessExpiresAt,
            refreshTokenRaw,
            refreshExpiresAt,
            _options.TokenType);
    }

    public async Task<RefreshOutcome?> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var hash = HashToken(refreshToken);
        var stored = await _dbContext.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (stored is null || !stored.IsActive || stored.User is null)
        {
            _logger.LogWarning("Geçersiz veya süresi dolmuş refresh token denemesi.");
            return null;
        }

        stored.RevokedAt = _clock.UtcNow;
        stored.RevokedByIp = ipAddress;
        stored.RevocationReason = "rotated";

        var roles = await _dbContext.UserRoles
            .Where(ur => ur.UserId == stored.UserId)
            .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
            .ToListAsync(cancellationToken);

        var extraClaims = new Dictionary<string, string>
        {
            [ClaimTypesExtended.MembershipStatus] = ((int)stored.User.MembershipStatus).ToString(),
            [ClaimTypesExtended.FullName] = stored.User.FullName
        };

        if (stored.User.PrimaryCompanyId is Guid companyId)
        {
            extraClaims[ClaimTypesExtended.CompanyId] = companyId.ToString();
        }

        var issued = await IssueAsync(stored.UserId, stored.User.Email ?? string.Empty, roles, extraClaims, cancellationToken);
        return new RefreshOutcome(stored.UserId, issued);
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(refreshToken);
        var stored = await _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (stored is null)
        {
            return;
        }

        stored.RevokedAt = _clock.UtcNow;
        stored.RevocationReason = "manual_revoke";
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
