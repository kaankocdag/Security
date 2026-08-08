using FluentAssertions;
using Kaan.SecurityPlatform.Infrastructure.Lab;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.Lab;

public sealed class LabNetworkPolicyValidatorTests
{
    private readonly LabNetworkPolicyValidator _sut = new();

    [Fact]
    public void ValidateTarget_InternalEndpoint_Succeeds()
    {
        var endpoint = "http://lab-abc.kaan-lab.internal:8080";
        var result = _sut.ValidateTarget(Guid.NewGuid(), endpoint, endpoint, null);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateTarget_AllowlistedExternalHost_Succeeds()
    {
        var result = _sut.ValidateTarget(
            Guid.NewGuid(),
            "https://example.com/",
            "http://lab-local:8080",
            "example.com");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateTarget_DifferentHost_Fails()
    {
        var result = _sut.ValidateTarget(
            Guid.NewGuid(),
            "http://evil.example:8080",
            "http://lab-abc.kaan-lab.internal:8080",
            "allowed.example");
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("lab_target_rejected");
    }
}
