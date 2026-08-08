using Kaan.SecurityPlatform.Domain.Entities.Findings;

namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

public interface ISecurityScoreCalculator
{
    SecurityScoreResult Calculate(IEnumerable<Finding> findings);
}

public sealed record SecurityScoreResult(
    int Score,
    int MaxScore,
    string Grade,
    IReadOnlyList<SecurityScoreDeduction> Deductions,
    string ExplanationTr);

public sealed record SecurityScoreDeduction(
    Guid? FindingId,
    string Title,
    int Impact,
    string Reason);
