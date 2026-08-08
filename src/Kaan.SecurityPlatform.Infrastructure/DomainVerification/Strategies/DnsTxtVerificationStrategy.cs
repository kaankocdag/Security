using DnsClient;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Kaan.SecurityPlatform.Infrastructure.DomainVerification.Strategies;

public sealed class DnsTxtVerificationStrategy : IVerificationStrategy
{
    private readonly ILookupClient _dns;
    private readonly DomainVerificationOptions _options;

    public DnsTxtVerificationStrategy(ILookupClient dns, IOptions<DomainVerificationOptions> options)
    {
        _dns = dns;
        _options = options.Value;
    }

    public VerificationMethod Method => VerificationMethod.DnsTxt;

    public async Task<DomainVerificationOutcome> VerifyAsync(string host, string expectedToken, CancellationToken cancellationToken)
    {
        var query = $"{_options.TxtRecordPrefix}.{host}";
        var response = await _dns.QueryAsync(query, QueryType.TXT, cancellationToken: cancellationToken);
        var value = response.Answers.TxtRecords()
            .SelectMany(r => r.Text)
            .Select(t => t.Trim())
            .FirstOrDefault(t => t.Equals(expectedToken, StringComparison.OrdinalIgnoreCase));

        if (value is null)
        {
            return new DomainVerificationOutcome(
                false, Method,
                ErrorCode: "txt_not_found",
                ErrorDetail: $"'{query}' TXT kaydında '{expectedToken}' değeri bulunamadı.");
        }

        return new DomainVerificationOutcome(true, Method, Evidence: $"TXT@{query}={value}");
    }
}
