using FluentAssertions;
using Kaan.SecurityPlatform.Application.Common.Interfaces.Lab;
using Kaan.SecurityPlatform.Application.Features.Lab;
using Kaan.SecurityPlatform.Infrastructure.Lab.Scenarios;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.Lab;

public sealed class LabScenarioRegistryTests
{
    private static LabScenarioRegistry CreateRegistry()
    {
        ILabScenario[] scenarios =
        [
            new InputValidationFailureScenario(),
            new OutputEncodingFailureScenario(),
            new InsecureSessionConfigScenario(),
            new BrokenAccessControlScenario(),
            new InsecureFileValidationScenario(),
            new InsecureJwtScenario(),
            new MissingSecurityHeadersScenario(),
            new UnsafeQueryConstructionScenario()
        ];
        return new LabScenarioRegistry(scenarios);
    }

    [Fact]
    public void GetAll_ContainsEightScenarios()
    {
        var registry = CreateRegistry();
        registry.GetAll().Should().HaveCount(8);
    }

    [Fact]
    public void IsRegistered_KnownAndUnknown()
    {
        var registry = CreateRegistry();
        registry.IsRegistered(LabScenarioKeys.MissingSecurityHeaders).Should().BeTrue();
        registry.IsRegistered("NotARealScenario").Should().BeFalse();
    }

    [Fact]
    public void GetSignedPlan_HasTenSteps()
    {
        var registry = CreateRegistry();
        var plan = registry.Get(LabScenarioKeys.InsecureJwtConfig)!.GetSignedPlan();
        plan.Steps.Should().HaveCount(10);
        plan.ScenarioKey.Should().Be(LabScenarioKeys.InsecureJwtConfig);
    }
}
