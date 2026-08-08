using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Options;

namespace Kaan.SecurityPlatform.Infrastructure.DomainVerification.Strategies;

public sealed class HtmlFileVerificationStrategy : IVerificationStrategy
{
    private readonly SecureHttpClientFactory _httpFactory;
    private readonly DomainVerificationOptions _options;

    public HtmlFileVerificationStrategy(
        SecureHttpClientFactory httpFactory,
        IOptions<DomainVerificationOptions> options)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
    }

    public VerificationMethod Method => VerificationMethod.HtmlFile;

    public async Task<DomainVerificationOutcome> VerifyAsync(string host, string expectedToken, CancellationToken cancellationToken)
    {
        using var client = _httpFactory.Create(timeout: TimeSpan.FromSeconds(_options.TimeoutSeconds));
        var url = $"https://{host}{_options.HtmlFilePath}";
        try
        {
            var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new DomainVerificationOutcome(false, Method,
                    ErrorCode: "http_status",
                    ErrorDetail: $"HTTP {(int)response.StatusCode} yanıt alındı: {url}");
            }

            var content = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();
            if (!string.Equals(content, expectedToken, StringComparison.OrdinalIgnoreCase))
            {
                return new DomainVerificationOutcome(false, Method,
                    ErrorCode: "token_mismatch",
                    ErrorDetail: "Dosya içeriği beklenen token ile eşleşmiyor.");
            }

            return new DomainVerificationOutcome(true, Method, Evidence: url);
        }
        catch (HttpRequestException ex)
        {
            return new DomainVerificationOutcome(false, Method,
                ErrorCode: "http_error",
                ErrorDetail: ex.Message);
        }
    }
}
