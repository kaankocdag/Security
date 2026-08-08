using FluentAssertions;
using Kaan.SecurityPlatform.Application.Features.Lab;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.Lab;

public sealed class AssessmentModeGuardTests
{
    private readonly AssessmentModeGuard _sut = new();

    [Fact]
    public void Supported_modes_succeed()
    {
        _sut.EnsureSupported(AssessmentMode.PublicPassiveAssessment).IsSuccess.Should().BeTrue();
        _sut.EnsureSupported(AssessmentMode.IsolatedSecurityLab).IsSuccess.Should().BeTrue();
        _sut.EnsureSupported(AssessmentMode.AuthorizedExternalAssessment).IsSuccess.Should().BeTrue();
        _sut.EnsureSupported(AssessmentMode.ApplicationSecurityCandidate).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void AuthorizedExternalAssessment_name_is_allowed()
    {
        var result = _sut.EnsureNameAllowed(AssessmentModeNames.AuthorizedExternalAssessment);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Legacy_active_aliases_are_rejected()
    {
        var result = _sut.EnsureNameAllowed("ActiveExternalAssessment");
        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be("assessment_mode_forbidden");
    }

    [Fact]
    public void Production_allows_three_modes()
    {
        _sut.EnsureEnvironmentAllows(AssessmentMode.PublicPassiveAssessment, "Production")
            .IsSuccess.Should().BeTrue();
        _sut.EnsureEnvironmentAllows(AssessmentMode.IsolatedSecurityLab, "Staging")
            .IsSuccess.Should().BeTrue();
        _sut.EnsureEnvironmentAllows(AssessmentMode.AuthorizedExternalAssessment, "Production")
            .IsSuccess.Should().BeTrue();
    }
}
