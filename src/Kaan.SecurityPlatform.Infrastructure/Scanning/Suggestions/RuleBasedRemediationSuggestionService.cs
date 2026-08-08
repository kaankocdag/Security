using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Domain.Entities.Findings;

namespace Kaan.SecurityPlatform.Infrastructure.Scanning.Suggestions;

public sealed class RuleBasedRemediationSuggestionService : IRemediationSuggestionService
{
    public Task<RemediationSuggestion?> BuildSuggestionAsync(Finding finding, CancellationToken cancellationToken = default)
    {
        if (finding is null)
        {
            return Task.FromResult<RemediationSuggestion?>(null);
        }

        var summary = string.IsNullOrWhiteSpace(finding.TurkishExecutiveSummary)
            ? finding.Description
            : finding.TurkishExecutiveSummary;

        var steps = string.IsNullOrWhiteSpace(finding.Remediation)
            ? "Bulgunun türüne göre düzeltme adımları henüz hazırlanmadı. Lütfen destek ekibiyle iletişime geçin."
            : finding.Remediation;

        return Task.FromResult<RemediationSuggestion?>(new RemediationSuggestion(
            summary,
            steps,
            finding.RemediationExampleConfig,
            EstimateDifficulty(finding),
            RequiresRetest: true));
    }

    private static string EstimateDifficulty(Finding finding) => finding.Severity switch
    {
        Domain.Enums.Severity.Critical => "Yüksek - hemen mühendislik ekibine iletin",
        Domain.Enums.Severity.High => "Orta-Yüksek - önceliklendirin",
        Domain.Enums.Severity.Medium => "Orta - haftalık ajandaya alın",
        Domain.Enums.Severity.Low => "Düşük - iyileştirme sprintinde yapılabilir",
        _ => "Düşük - bilgilendirme"
    };
}
