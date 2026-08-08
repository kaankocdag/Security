using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Features.Auth.Dtos;

public sealed record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string CompanyName,
    string? CompanyContactPhone,
    string? CompanyWebsiteUrl,
    string? CompanyIndustry,
    string? JobTitle,
    bool AcceptTerms);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record RevokeRequest(string RefreshToken);

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    string TokenType,
    CurrentUserDto User);

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string FullName,
    Guid? CompanyId,
    string? CompanyName,
    MembershipStatus MembershipStatus,
    IReadOnlyCollection<string> Roles,
    string? AvatarPath);

public sealed record RegisterResponse(
    Guid UserId,
    Guid CompanyId,
    MembershipStatus MembershipStatus,
    string Message);
