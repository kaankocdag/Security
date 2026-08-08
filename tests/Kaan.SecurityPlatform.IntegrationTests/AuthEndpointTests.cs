using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Kaan.SecurityPlatform.IntegrationTests;

public sealed class AuthEndpointTests : IClassFixture<KaanApiWebApplicationFactory>
{
    private readonly KaanApiWebApplicationFactory _factory;

    public AuthEndpointTests(KaanApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Kayit_endpoint_erisilebilir()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "yeni.uye@example.com",
            password = "SuperGuvenli!2026",
            fullName = "Yeni Üye",
            companyName = "Test Firma A.Ş.",
            companyDomain = "test.local"
        });
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Onay_gerektiren_endpoint_401_veya_403_doner()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/projects");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Swagger_endpoint_uretilir()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
