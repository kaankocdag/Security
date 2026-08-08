using System.Text;
using System.Text.RegularExpressions;
using Kaan.SecurityPlatform.Application.Features.HackerOne;

namespace Kaan.SecurityPlatform.Infrastructure.HackerOne;

/// <summary>HackerOne export is always English (en-US) — independent from UI language.</summary>
public sealed class HackerOneMarkdownBuilder : IHackerOneMarkdownBuilder
{
    public string Build(HackerOneReportDraftFields fields, bool preferEnglish = true)
    {
        _ = preferEnglish;
        var language = string.IsNullOrWhiteSpace(fields.Language)
            ? HackerOneReportLanguage.Code
            : fields.Language;

        var sb = new StringBuilder();
        sb.AppendLine($"# {fields.Title}");
        sb.AppendLine();
        sb.AppendLine($"**Language:** {language}");
        sb.AppendLine($"**Finding Type:** {fields.FindingType ?? "Finding"}");
        sb.AppendLine($"**Candidate Severity:** {fields.CandidateSeverity ?? fields.Severity}");
        sb.AppendLine($"**Bug bounty severity:** {fields.BugBountySeverityLabel}");
        sb.AppendLine($"**Confirmed Vulnerability:** {(fields.ConfirmedVulnerability ? "Yes" : "No")}");
        sb.AppendLine($"**{fields.Weakness}**");
        sb.AppendLine($"**Exploitability:** {fields.ExploitabilityLabel ?? "Requires Manual Validation"}");
        sb.AppendLine($"**Demonstrated Impact:** {(fields.DemonstratedImpact ? "Yes" : "No")}");
        sb.AppendLine($"**Submission Recommendation:** {fields.SubmissionRecommendationLabel ?? "Manual Review"}");
        sb.AppendLine($"**Asset:** {fields.Asset}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(fields.Summary))
        {
            sb.AppendLine("## Summary");
            sb.AppendLine(fields.Summary);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(fields.VulnerabilityInformation))
        {
            sb.AppendLine("## Vulnerability Information");
            sb.AppendLine(fields.VulnerabilityInformation);
            sb.AppendLine();
        }

        sb.AppendLine("## Impact");
        sb.AppendLine(StripSurfaceEvidenceSections(fields.Impact));
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(fields.SurfaceEvidence))
        {
            sb.AppendLine("## Surface Evidence");
            sb.AppendLine(StripSurfaceEvidenceHeading(fields.SurfaceEvidence));
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(fields.ValidationEvidence))
        {
            sb.AppendLine("## Validation Evidence");
            sb.AppendLine(fields.ValidationEvidence.Trim());
            sb.AppendLine();
        }
        else if (fields.ConfirmedVulnerability != true)
        {
            sb.AppendLine("## Validation Evidence");
            sb.AppendLine(
                "This is a vulnerability candidate only. No unauthorized access or demonstrated security impact has been confirmed. " +
                "Do not submit solely on the basis of path existence or analyzer signals.");
            sb.AppendLine();
        }

        sb.AppendLine("## Steps to Reproduce");
        sb.AppendLine(SanitizeSteps(StripSurfaceEvidenceSections(fields.StepsToReproduce)));
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(fields.ExpectedResult))
        {
            sb.AppendLine("## Expected Result");
            sb.AppendLine(StripSurfaceEvidenceSections(fields.ExpectedResult));
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(fields.ActualResult))
        {
            sb.AppendLine("## Actual Result");
            sb.AppendLine(StripSurfaceEvidenceSections(fields.ActualResult));
            sb.AppendLine();
        }

        sb.AppendLine("## Proof of Concept");
        sb.AppendLine(StripSurfaceEvidenceSections(fields.ProofOfConcept));
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(fields.SuggestedRemediation))
        {
            sb.AppendLine("## Suggested Remediation");
            sb.AppendLine(fields.SuggestedRemediation);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(fields.TestingNotes))
        {
            sb.AppendLine("## Testing Notes");
            sb.AppendLine(fields.TestingNotes);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(fields.EligibilityReason))
        {
            sb.AppendLine("## Eligibility Reason");
            sb.AppendLine(fields.EligibilityReason);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(fields.Notes))
        {
            sb.AppendLine("## Notes");
            sb.AppendLine(fields.Notes);
        }

        return sb.ToString().TrimEnd() + Environment.NewLine;
    }

    public string BuildTurkish(HackerOneReportDraftFields fields)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {fields.Title}");
        sb.AppendLine();
        sb.AppendLine("**Dil:** tr-TR (yalnızca iç inceleme — HackerOne’a gönderilmez)");
        sb.AppendLine($"**Bulgu tipi:** {fields.FindingType ?? "Bulgu"}");
        sb.AppendLine($"**Aday şiddet:** {fields.CandidateSeverity ?? fields.Severity}");
        sb.AppendLine($"**Bug bounty şiddeti:** {fields.BugBountySeverityLabel}");
        sb.AppendLine($"**Doğrulanmış zafiyet:** {(fields.ConfirmedVulnerability ? "Evet" : "Hayır")}");
        sb.AppendLine($"**{fields.Weakness}**");
        sb.AppendLine($"**Sömürülebilirlik:** {fields.ExploitabilityLabel ?? "Manuel doğrulama gerekir"}");
        sb.AppendLine($"**Kanıtlanmış etki:** {(fields.DemonstratedImpact ? "Evet" : "Hayır")}");
        sb.AppendLine($"**Gönderim önerisi:** {fields.SubmissionRecommendationLabel ?? "Manuel inceleme"}");
        sb.AppendLine($"**Varlık:** {fields.Asset}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(fields.Summary))
        {
            sb.AppendLine("## Özet");
            sb.AppendLine(fields.Summary);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(fields.VulnerabilityInformation))
        {
            sb.AppendLine("## Bulgu bilgisi");
            sb.AppendLine(fields.VulnerabilityInformation);
            sb.AppendLine();
        }

        sb.AppendLine("## Etki");
        sb.AppendLine(StripSurfaceEvidenceSections(fields.Impact));
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(fields.SurfaceEvidence))
        {
            sb.AppendLine("## Surface Evidence");
            sb.AppendLine(StripSurfaceEvidenceHeading(fields.SurfaceEvidence));
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(fields.ValidationEvidence))
        {
            sb.AppendLine("## Validation Evidence");
            sb.AppendLine(fields.ValidationEvidence.Trim());
            sb.AppendLine();
        }

        sb.AppendLine("## Yeniden üretme adımları");
        sb.AppendLine(SanitizeSteps(StripSurfaceEvidenceSections(fields.StepsToReproduce)));
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(fields.ExpectedResult))
        {
            sb.AppendLine("## Beklenen sonuç");
            sb.AppendLine(StripSurfaceEvidenceSections(fields.ExpectedResult));
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(fields.ActualResult))
        {
            sb.AppendLine("## Gerçekleşen sonuç");
            sb.AppendLine(StripSurfaceEvidenceSections(fields.ActualResult));
            sb.AppendLine();
        }

        sb.AppendLine("## Kanıt (PoC)");
        sb.AppendLine(StripSurfaceEvidenceSections(fields.ProofOfConcept));
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(fields.SuggestedRemediation))
        {
            sb.AppendLine("## Önerilen düzeltme");
            sb.AppendLine(fields.SuggestedRemediation);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(fields.TestingNotes))
        {
            sb.AppendLine("## Test notları");
            sb.AppendLine(fields.TestingNotes);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(fields.EligibilityReason))
        {
            sb.AppendLine("## Uygunluk gerekçesi");
            sb.AppendLine(fields.EligibilityReason);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(fields.Notes))
        {
            sb.AppendLine("## Notlar");
            sb.AppendLine(fields.Notes);
        }

        return sb.ToString().TrimEnd() + Environment.NewLine;
    }

    public int ComputeReadinessScore(HackerOneReportDraftFields fields)
    {
        var score = 100;
        score -= Penalty(fields.Title, 20, 8);
        score -= Penalty(fields.Severity, 10, 5);
        score -= Penalty(fields.Asset, 15, 8);
        score -= Penalty(fields.Weakness, 10, 5);
        score -= Penalty(fields.Impact, 20, 40);
        score -= Penalty(fields.StepsToReproduce, 15, 40);
        score -= Penalty(fields.ProofOfConcept, 10, 20);
        return Math.Clamp(score, 0, 100);
    }

    public string FormatSafeUrlForSteps(string? urlOrHost)
    {
        if (string.IsNullOrWhiteSpace(urlOrHost))
        {
            return "`(asset unavailable)`";
        }

        var value = urlOrHost.Trim();
        if (value.Contains("[redacted]", StringComparison.OrdinalIgnoreCase)
            || value.Contains("?[redacted]", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(value, @"\?\[[^\]]+\]"))
        {
            return $"`{EscapeBackticks(value)}`";
        }

        return $"`{EscapeBackticks(value)}`";
    }

    private static string SanitizeSteps(string steps)
    {
        if (string.IsNullOrWhiteSpace(steps))
        {
            return steps;
        }

        var sanitized = Regex.Replace(
            steps,
            @"\[([^\]]*)\]\(([^)]*\[redacted\][^)]*)\)",
            m => $"`{EscapeBackticks(m.Groups[2].Value)}`",
            RegexOptions.IgnoreCase);

        sanitized = Regex.Replace(
            sanitized,
            @"(?<!`)(https?://[^\s)]+\?\[redacted\])(?!`)",
            m => $"`{m.Groups[1].Value}`",
            RegexOptions.IgnoreCase);

        // Never ship generic placeholder reproduce text in HackerOne exports.
        sanitized = Regex.Replace(
            sanitized,
            @"(?im)^\s*\d+\.\s*Reproduce the described candidate behavior\.?\s*$",
            string.Empty);
        sanitized = sanitized.Replace(
            "Reproduce the described candidate behavior.",
            "Use the recorded HTTP observations in Surface Evidence.",
            StringComparison.OrdinalIgnoreCase);

        return sanitized.Trim();
    }

    private static string EscapeBackticks(string value) => value.Replace('`', '\'');

    private static string StripSurfaceEvidenceHeading(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("## Surface Evidence", StringComparison.OrdinalIgnoreCase))
        {
            var nl = trimmed.IndexOf('\n');
            return nl >= 0 ? trimmed[(nl + 1)..].Trim() : string.Empty;
        }

        return trimmed;
    }

    /// <summary>Ensure Surface Evidence appears only in its dedicated markdown section.</summary>
    private static string StripSurfaceEvidenceSections(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value ?? string.Empty;
        }

        var cleaned = Regex.Replace(
            value,
            @"##\s*Surface Evidence\b[\s\S]*?(?=(\n##\s|\z))",
            string.Empty,
            RegexOptions.IgnoreCase);
        return cleaned.Trim();
    }

    private static int Penalty(string? value, int emptyPenalty, int minLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return emptyPenalty;
        }

        return value.Trim().Length < minLength ? emptyPenalty / 2 : 0;
    }
}
