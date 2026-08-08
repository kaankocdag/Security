using DnsClient;
using Kaan.SecurityPlatform.Application.Features.HackerOne;
using Kaan.SecurityPlatform.Infrastructure.HackerOne.Engines;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Http;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Safety;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.Scanning;

public class DetectionEngineSafetyTests
{
    private static readonly SecureHttpClientFactory HttpFactory = new(
        new TargetSafetyValidator(NullLogger<TargetSafetyValidator>.Instance, new LookupClient()),
        NullLogger<SecureHttpClientFactory>.Instance);

    private static IApplicationSecurityCandidateEngine[] CreateEngines() =>
    [
        new SubdomainTakeoverCandidateEngine(HttpFactory, NullLogger<SubdomainTakeoverCandidateEngine>.Instance),
        new JsSecretExposureCandidateEngine(HttpFactory, NullLogger<JsSecretExposureCandidateEngine>.Instance),
        new ApiSurfaceCandidateEngine(HttpFactory, NullLogger<ApiSurfaceCandidateEngine>.Instance),
        new OpenRedirectCandidateEngine(HttpFactory, NullLogger<OpenRedirectCandidateEngine>.Instance)
    ];

    [Fact]
    public void Engine_keys_are_unique_and_non_empty()
    {
        var keys = CreateEngines().Select(e => e.EngineKey).ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.All(keys, k => Assert.False(string.IsNullOrWhiteSpace(k)));
    }

    [Fact]
    public void Expected_detection_engines_are_present()
    {
        var keys = CreateEngines().Select(e => e.EngineKey).ToHashSet();

        Assert.Contains("subdomain-takeover", keys);
        Assert.Contains("js-secret-exposure", keys);
        Assert.Contains("api-surface", keys);
        Assert.Contains("open-redirect", keys);
    }
}
