using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Kaan.SecurityPlatform.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<KaanApiWebApplicationFactory>
{
    private readonly KaanApiWebApplicationFactory _factory;

    public HealthEndpointTests(KaanApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_endpoint_200_doner()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
