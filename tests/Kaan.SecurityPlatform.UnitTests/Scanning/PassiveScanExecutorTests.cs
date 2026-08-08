using FluentAssertions;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.Scanning;

public sealed class SecurityScoreCalculatorAdditionalTests
{
    [Fact]
    public void Coklu_bulgu_gruplandirilir_ve_puan_tabani_uygulanir()
    {
        var calc = new SecurityScoreCalculator();
        var findings = Enumerable.Range(0, 10).Select(i => new Kaan.SecurityPlatform.Domain.Entities.Findings.Finding
        {
            Title = "T" + i,
            Description = "d",
            Category = "c",
            CheckCode = "check",
            Severity = Severity.High,
            TechnicalSeverity = Severity.High,
            FindingClass = FindingClass.Vulnerability,
            ConfidenceLevel = ConfidenceLevel.Confirmed
        }).ToArray();

        var result = calc.Calculate(findings);
        result.Score.Should().BeGreaterThanOrEqualTo(0);
        result.Score.Should().BeLessThan(50);
    }
}
