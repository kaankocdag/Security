using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Kaan.SecurityPlatform.Infrastructure.DomainVerification;

public interface IVerificationStrategy
{
    VerificationMethod Method { get; }
    Task<DomainVerificationOutcome> VerifyAsync(string host, string expectedToken, CancellationToken cancellationToken);
}

public sealed class CompositeDomainVerificationService : IDomainVerificationService
{
    private readonly IReadOnlyDictionary<VerificationMethod, IVerificationStrategy> _strategies;
    private readonly ILogger<CompositeDomainVerificationService> _logger;

    public CompositeDomainVerificationService(
        IEnumerable<IVerificationStrategy> strategies,
        ILogger<CompositeDomainVerificationService> logger)
    {
        _strategies = strategies.ToDictionary(s => s.Method, s => s);
        _logger = logger;
    }

    public async Task<DomainVerificationOutcome> VerifyAsync(
        string normalizedHostName,
        string expectedToken,
        VerificationMethod method,
        CancellationToken cancellationToken = default)
    {
        if (!_strategies.TryGetValue(method, out var strategy))
        {
            _logger.LogWarning("Domain doğrulama stratejisi bulunamadı: {Method}", method);
            return new DomainVerificationOutcome(
                false,
                method,
                ErrorCode: "strategy_not_found",
                ErrorDetail: $"'{method}' doğrulama yöntemi kayıtlı değil.");
        }

        try
        {
            return await strategy.VerifyAsync(normalizedHostName, expectedToken, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Domain doğrulama hatası: {Host} {Method}", normalizedHostName, method);
            return new DomainVerificationOutcome(
                false,
                method,
                ErrorCode: "verification_exception",
                ErrorDetail: ex.Message);
        }
    }
}
