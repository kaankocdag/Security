using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Kaan.SecurityPlatform.IntegrationTests;

public sealed class LabEndpointTests : IClassFixture<KaanApiWebApplicationFactory>
{
    private readonly KaanApiWebApplicationFactory _factory;

    public LabEndpointTests(KaanApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Lab_scenarios_auth_yoksa_401_veya_403()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/admin/lab/scenarios");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Lab_start_auth_yoksa_401_veya_403()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/admin/lab/executions", new
        {
            scenarioKey = "MissingSecurityHeaders",
            confirmPhrase = "LABORATUVAR SENARYOSUNU BASLATMAYI ONAYLIYORUM",
            elevationToken = "dummy"
        });
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Lab_elevation_auth_yoksa_401_veya_403()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/admin/lab/elevation", new { password = "x" });
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }
}
