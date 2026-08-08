using FluentAssertions;
using Kaan.SecurityPlatform.Infrastructure.Lab;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.Lab;

public sealed class LabStartRequestGuardTests
{
    private readonly LabStartRequestGuard _sut = new();

    [Fact]
    public void Validate_AllowedFields_Succeeds()
    {
        var result = _sut.ValidateNoForbiddenFields(new Dictionary<string, object?>
        {
            ["scenarioKey"] = "MissingSecurityHeaders",
            ["confirmPhrase"] = "x",
            ["elevationToken"] = "y"
        });

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("url")]
    [InlineData("host")]
    [InlineData("payload")]
    [InlineData("IP")]
    public void Validate_ForbiddenFields_Fails(string field)
    {
        var result = _sut.ValidateNoForbiddenFields(new Dictionary<string, object?>
        {
            ["scenarioKey"] = "MissingSecurityHeaders",
            [field] = "evil"
        });

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("lab_forbidden_field");
    }
}
