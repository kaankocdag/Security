namespace Kaan.SecurityPlatform.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "kaan-security-platform";
    public string Audience { get; set; } = "kaan-security-platform-clients";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 30;
    public int RefreshTokenLifetimeDays { get; set; } = 14;
    public string TokenType { get; set; } = "Bearer";
    public int ClockSkewSeconds { get; set; } = 60;
}
