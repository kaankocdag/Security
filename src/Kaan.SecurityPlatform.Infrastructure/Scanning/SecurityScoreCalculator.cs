using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning;

/// <summary>
/// Açıklanabilir güvenlik puanı hesaplayıcı.
/// Ceza modelinde temel ağırlık severity + confidence ile çarpılır.
/// </summary>
public sealed class SecurityScoreCalculator : ISecurityScoreCalculator
{
    private const int MaxScore = 100;

    private static readonly IReadOnlyDictionary<Severity, int> BaseDeductions = new Dictionary<Severity, int>
    {
        [Severity.Critical] = 25,
        [Severity.High] = 15,
        [Severity.Medium] = 8,
        [Severity.Low] = 3,
        [Severity.Informational] = 1
    };

    private static readonly IReadOnlyDictionary<ConfidenceLevel, double> ConfidenceMultipliers = new Dictionary<ConfidenceLevel, double>
    {
        [ConfidenceLevel.Confirmed] = 1.0,
        [ConfidenceLevel.StrongIndication] = 0.65,
        [ConfidenceLevel.Recommendation] = 0.35
    };

    public SecurityScoreResult Calculate(IEnumerable<Finding> findings)
    {
        var relevant = findings
            .Where(f => f.Status is FindingStatus.Open or FindingStatus.InProgress or FindingStatus.Reopened)
            .Where(f => !f.IsFalsePositive)
            .ToList();

        var deductions = new List<SecurityScoreDeduction>();
        var total = 0;

        // Puan TechnicalSeverity ile hesaplanır (scanner Severity'den bağımsız doğrulama katmanı).
        foreach (var finding in relevant.OrderByDescending(f => f.TechnicalSeverity))
        {
            if (!BaseDeductions.TryGetValue(finding.TechnicalSeverity, out var baseImpact))
            {
                continue;
            }

            // Hardening / SEO / Informational / Candidate bulgular puanı şişirmesin
            if (finding.FindingClass is FindingClass.SeoIssue or FindingClass.Informational
                or FindingClass.VulnerabilityCandidate)
            {
                baseImpact = Math.Min(baseImpact, 1);
            }
            else if (finding.FindingClass == FindingClass.HardeningRecommendation)
            {
                baseImpact = Math.Min(baseImpact, 3);
            }

            if (!ConfidenceMultipliers.TryGetValue(finding.ConfidenceLevel, out var multiplier))
            {
                multiplier = 0.35;
            }

            var impact = (int)Math.Ceiling(baseImpact * multiplier);
            total += impact;
            finding.ScoreImpact = impact;

            deductions.Add(new SecurityScoreDeduction(
                finding.Id,
                finding.Title,
                impact,
                $"Tech={finding.TechnicalSeverity} / Scan={finding.Severity} / {finding.FindingClass}"));
        }

        var score = Math.Clamp(MaxScore - total, 0, MaxScore);
        var grade = score switch
        {
            >= 90 => "A",
            >= 80 => "B",
            >= 70 => "C",
            >= 60 => "D",
            >= 50 => "E",
            _ => "F"
        };

        var explanation = $"Toplam {relevant.Count} açık bulgu değerlendirildi. " +
                          $"Cezalar toplamı: {total}. Nihai puan: {score}/100 (Not: {grade})";

        return new SecurityScoreResult(score, MaxScore, grade, deductions, explanation);
    }
}
