using Kaan.SecurityPlatform.Domain.Entities.Findings;

namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

public interface IRemediationSuggestionService
{
    Task<RemediationSuggestion?> BuildSuggestionAsync(Finding finding, CancellationToken cancellationToken = default);
}

public sealed record RemediationSuggestion(
    string SummaryTr,
    string StepByStepTr,
    string? ExampleConfig,
    string? EstimatedDifficulty,
    bool RequiresRetest);
