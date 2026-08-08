using System.Net;
using DnsClient;
using FluentAssertions;
using Kaan.SecurityPlatform.Infrastructure.Scanning.Safety;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.Scanning;

public sealed class TargetSafetyValidatorTests
{
    private readonly TargetSafetyValidator _validator = new(
        NullLogger<TargetSafetyValidator>.Instance,
        new LookupClient(new LookupClientOptions { UseCache = false, Timeout = TimeSpan.FromSeconds(1) }));

    [Theory]
    [InlineData("http://localhost/", "forbidden_host")]
    [InlineData("http://127.0.0.1/", "loopback_ip")]
    [InlineData("http://192.168.1.1/", "unsafe_private_192")]
    [InlineData("http://10.0.0.5/", "unsafe_private_10")]
    [InlineData("http://169.254.169.254/latest/meta-data", "forbidden_host")]
    [InlineData("http://metadata.google.internal/", "forbidden_host")]
    [InlineData("file:///etc/passwd", "forbidden_scheme")]
    [InlineData("ftp://example.com/", "forbidden_scheme")]
    public void Yasak_hedefler_reddedilir(string url, string expectedReasonPrefix)
    {
        var result = _validator.ValidateUri(new Uri(url));
        result.IsSafe.Should().BeFalse();
        result.ReasonCode.Should().StartWith(expectedReasonPrefix);
    }

    [Theory]
    [InlineData("https://example.com/")]
    [InlineData("http://kaansecurity.local/status")]
    public void Public_hedefler_izin_verilir(string url)
    {
        var result = _validator.ValidateUri(new Uri(url));
        result.IsSafe.Should().BeTrue();
    }

    [Fact]
    public void Ozel_ip_ler_reddedilir()
    {
        _validator.ValidateResolvedIp(IPAddress.Parse("10.0.0.1")).IsSafe.Should().BeFalse();
        _validator.ValidateResolvedIp(IPAddress.Parse("172.16.0.1")).IsSafe.Should().BeFalse();
        _validator.ValidateResolvedIp(IPAddress.Parse("192.168.1.1")).IsSafe.Should().BeFalse();
        _validator.ValidateResolvedIp(IPAddress.Parse("100.64.0.1")).IsSafe.Should().BeFalse();
        _validator.ValidateResolvedIp(IPAddress.Parse("8.8.8.8")).IsSafe.Should().BeTrue();
    }

    [Theory]
    [InlineData("*.example.com")]
    [InlineData("api.*.example.com")]
    public void Wildcard_host_reddedilir(string host)
    {
        var result = _validator.ValidateHost(host);
        result.IsSafe.Should().BeFalse();
        result.ReasonCode.Should().Be("wildcard_or_invalid_host");
    }
}
