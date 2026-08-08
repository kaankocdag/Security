using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Microsoft.Extensions.Options;

namespace Kaan.SecurityPlatform.Infrastructure.HackerOne;

/// <summary>API kapalıyken veya token yokken kullanılan güvenli stub.</summary>
public sealed class NullHackerOneApiClient : IHackerOneApiClient
{
    private readonly HackerOneOptions _options;

    public NullHackerOneApiClient(IOptions<HackerOneOptions> options)
    {
        _options = options.Value;
    }

    public bool IsEnabled => false;

    public Task<Result<IReadOnlyList<HackerOneRemoteProgram>>> ListProgramsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result<IReadOnlyList<HackerOneRemoteProgram>>.Failure(
            "hackerone_api_disabled",
            "HackerOne API kapalı (HackerOne:ApiEnabled=false) veya istemci yapılandırılmadı."));

    public Task<Result<IReadOnlyList<HackerOneRemoteScope>>> ListStructuredScopesAsync(
        string programHandle,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Result<IReadOnlyList<HackerOneRemoteScope>>.Failure(
            "hackerone_api_disabled",
            "HackerOne API kapalı (HackerOne:ApiEnabled=false) veya istemci yapılandırılmadı."));

    public Task<Result<HackerOneRemoteSubmission>> SubmitReportAsync(HackerOneSubmitPayload payload, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<HackerOneRemoteSubmission>.Failure(
            "hackerone_api_disabled",
            "HackerOne API kapalı. Copy Full Report / Open HackerOne kullanın."));
}
