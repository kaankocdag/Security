using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Features.Domains.Dtos;

public sealed record DomainListItemDto(
    Guid Id,
    Guid SecurityProjectId,
    string HostName,
    string Scheme,
    bool IsVerified,
    DomainAssetStatus Status,
    DateTime? VerifiedAt,
    DateTime CreatedAt,
    string Source = "Manual",
    string? HackerOneProgramHandle = null,
    string? HackerOneProgramName = null,
    bool? HackerOneEligibleForBounty = null,
    bool? HackerOneOffersBounties = null,
    string? HackerOneCurrency = null,
    string? HackerOneMaxSeverity = null,
    string? HackerOneBountySummary = null,
    bool HackerOneIsWildcard = false,
    string? HackerOneAssetType = null);

public sealed record DomainDetailDto(
    Guid Id,
    Guid CompanyId,
    Guid SecurityProjectId,
    string HostName,
    string NormalizedHostName,
    string Scheme,
    int? Port,
    bool IsVerified,
    DomainAssetStatus Status,
    VerificationMethod? VerificationMethod,
    string? VerificationToken,
    DateTime? VerifiedAt,
    string? LastVerificationError,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateDomainRequest(
    Guid SecurityProjectId,
    string HostName,
    string Scheme = "https",
    int? Port = null);

public sealed record StartVerificationRequest(
    Guid DomainId,
    VerificationMethod Method);

public sealed record StartVerificationResponse(
    Guid DomainId,
    VerificationMethod Method,
    string Token,
    string Instruction);

public sealed record RunVerificationResponse(
    Guid DomainId,
    bool IsVerified,
    VerificationMethod Method,
    string? Evidence,
    string? ErrorCode,
    string? ErrorDetail);

public sealed record SetVerificationManualRequest(
    Guid DomainId,
    bool IsVerified,
    string? Note = null);

public sealed record SetVerificationManualResponse(
    Guid DomainId,
    bool IsVerified,
    DomainAssetStatus Status,
    DateTime? VerifiedAt);
