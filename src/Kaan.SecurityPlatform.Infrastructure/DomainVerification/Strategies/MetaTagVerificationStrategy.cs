using AngleSharp;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Microsoft.Extensions.Options;

namespace Kaan.SecurityPlatform.Infrastructure.DomainVerification.Strategies;

public sealed class MetaTagVerificationStrategy : IVerificationStrategy
{
    private readonly SecureHttpClientFactory _httpFactory;
    private readonly DomainVerificationOptions _options;

    public MetaTagVerificationStrategy(
        SecureHttpClientFactory httpFactory,
        IOptions<DomainVerificationOptions> options)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
    }

    public VerificationMethod Method => VerificationMethod.MetaTag;

    public async Task<DomainVerificationOutcome> VerifyAsync(string host, string expectedToken, CancellationToken cancellationToken)
    {
        using var client = _httpFactory.Create(timeout: TimeSpan.FromSeconds(_options.TimeoutSeconds));
        var url = $"https://{host}/";

        try
        {
            var html = await client.GetStringAsync(url, cancellationToken);
            var config = Configuration.Default;
            using var context = BrowsingContext.New(config);
            var document = await context.OpenAsync(req => req.Content(html), cancellationToken);
            var meta = document.QuerySelector($"meta[name='{_options.MetaTagName}']");
            var content = meta?.GetAttribute("content")?.Trim();

            if (string.IsNullOrEmpty(content) || !string.Equals(content, expectedToken, StringComparison.OrdinalIgnoreCase))
            {
                return new DomainVerificationOutcome(false, Method,
                    ErrorCode: "meta_not_found",
                    ErrorDetail: $"'{_options.MetaTagName}' meta etiketi bulunamadı veya değer uyuşmuyor.");
            }

            return new DomainVerificationOutcome(true, Method, Evidence: $"meta:{_options.MetaTagName}={content}");
        }
        catch (HttpRequestException ex)
        {
            return new DomainVerificationOutcome(false, Method,
                ErrorCode: "http_error",
                ErrorDetail: ex.Message);
        }
    }
}
