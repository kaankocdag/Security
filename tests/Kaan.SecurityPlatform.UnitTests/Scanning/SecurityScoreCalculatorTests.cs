using FluentAssertions;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Scanning;
using Xunit;

namespace Kaan.SecurityPlatform.UnitTests.Scanning;

public sealed class SecurityScoreCalculatorTests
{
    private readonly SecurityScoreCalculator _calculator = new();

    [Fact]
    public void Bos_bulgu_listesinde_puan_100_olur()
    {
        var result = _calculator.Calculate(Array.Empty<Finding>());
        result.Score.Should().Be(100);
        result.Grade.Should().Be("A");
    }

    [Fact]
    public void Kritik_confirmed_bulgu_puanin_dusmesine_neden_olur()
    {
        var findings = new[]
        {
            new Finding
            {
                Title = "Test",
                Description = "d",
                Category = "test",
                CheckCode = "check.test",
                Severity = Severity.Critical,
                ConfidenceLevel = ConfidenceLevel.Confirmed,
                Status = FindingStatus.Open
            }
        };

        var result = _calculator.Calculate(findings);
        result.Score.Should().BeLessThan(100);
        result.Deductions.Should().HaveCount(1);
    }

    [Fact]
    public void False_positive_bulgular_puani_etkilemez()
    {
        var findings = new[]
        {
            new Finding
            {
                Title = "yanlis",
                Description = "d",
                Category = "test",
                CheckCode = "check.test",
                Severity = Severity.Critical,
                ConfidenceLevel = ConfidenceLevel.Confirmed,
                Status = FindingStatus.Open,
                IsFalsePositive = true
            }
        };

        var result = _calculator.Calculate(findings);
        result.Score.Should().Be(100);
    }
}
