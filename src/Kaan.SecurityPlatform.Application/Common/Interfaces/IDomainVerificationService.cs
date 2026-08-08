using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

public interface IDomainVerificationService
{
    Task<DomainVerificationOutcome> VerifyAsync(
        string normalizedHostName,
        string expectedToken,
        VerificationMethod method,
        CancellationToken cancellationToken = default);
}

public sealed record DomainVerificationOutcome(
    bool IsVerified,
    VerificationMethod Method,
    string? Evidence = null,
    string? ErrorCode = null,
    string? ErrorDetail = null);
