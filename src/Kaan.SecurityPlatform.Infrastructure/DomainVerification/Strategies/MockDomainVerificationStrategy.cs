using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Kaan.SecurityPlatform.Infrastructure.DomainVerification.Strategies;

/// <summary>
/// Sadece geliştirme ortamında etkinleştirilen sahte doğrulayıcı.
/// Yapılandırmada tanımlı MockAutoApproveToken değerini bekler.
/// </summary>
public sealed class MockDomainVerificationStrategy : IVerificationStrategy
{
    private readonly DomainVerificationOptions _options;

    public MockDomainVerificationStrategy(IOptions<DomainVerificationOptions> options)
    {
        _options = options.Value;
    }

    public VerificationMethod Method => VerificationMethod.Mock;

    public Task<DomainVerificationOutcome> VerifyAsync(string host, string expectedToken, CancellationToken cancellationToken)
    {
        if (!_options.EnableMockStrategy)
        {
            return Task.FromResult(new DomainVerificationOutcome(
                false, Method,
                ErrorCode: "mock_disabled",
                ErrorDetail: "Sahte doğrulama devre dışı."));
        }

        if (string.Equals(expectedToken, _options.MockAutoApproveToken, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new DomainVerificationOutcome(true, Method,
                Evidence: $"mock:auto-approved:{host}"));
        }

        return Task.FromResult(new DomainVerificationOutcome(false, Method,
            ErrorCode: "mock_token_mismatch",
            ErrorDetail: "Sahte doğrulama için beklenen token değeri eşleşmedi."));
    }
}
