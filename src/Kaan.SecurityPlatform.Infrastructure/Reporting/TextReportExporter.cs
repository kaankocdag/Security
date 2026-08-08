using System.Globalization;
using System.Text;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Features.Reports;
using Kaan.SecurityPlatform.Domain.Entities.Scans;

namespace Kaan.SecurityPlatform.Infrastructure.Reporting;

public sealed class TextReportExporter : IReportExporter
{
    public string Format => "txt";

    public Task<ExportedReport> ExportAsync(
        ScanResult scanResult,
        ReportExportOptions options,
        CancellationToken cancellationToken = default)
    {
        var lang = ReportLanguageParser.Parse(options.LanguageCode);
        var copy = ReportCopy.For(lang);
        var host = scanResult.ScanJob?.DomainAsset?.HostName
            ?? scanResult.ScanJob?.DomainAsset?.NormalizedHostName
            ?? (lang == ReportLanguage.En ? "unknown-target" : "bilinmeyen-hedef");
        var culture = CultureInfo.GetCultureInfo(lang == ReportLanguage.En ? "en-US" : "tr-TR");
        var findings = (scanResult.Findings ?? Array.Empty<Domain.Entities.Findings.Finding>())
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.Title)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine("================================================================================");
        sb.AppendLine($"KAAN SECURITY PLATFORM — {copy.DocumentTitle.ToUpperInvariant()}");
        sb.AppendLine(copy.VendorSubtitle);
        sb.AppendLine($"Language / Dil: {copy.LanguageTag.ToUpperInvariant()}");
        sb.AppendLine("================================================================================");
        sb.AppendLine();
        sb.AppendLine(copy.CoverHeading);
        sb.AppendLine("--------------------------------------------------------------------------------");
        sb.AppendLine($"{copy.TargetDomain,-22}: {host}");
        sb.AppendLine($"{copy.ScanJobIdLabel,-22}: {scanResult.ScanJobId}");
        sb.AppendLine($"{copy.ReportDate,-22}: {scanResult.CompletedAt.ToString("dd MMMM yyyy HH:mm", culture)} UTC");
        sb.AppendLine($"{copy.SecurityScore,-22}: {scanResult.SecurityScore}/100");
        sb.AppendLine(
            $"{copy.Checks,-22}: {scanResult.ChecksPassed} {copy.Passed} / {scanResult.ChecksFailed} {copy.Issues} / {scanResult.ChecksSkipped} {copy.Skipped} ({copy.Total} {scanResult.ChecksTotal})");
        sb.AppendLine(
            $"{copy.FindingSummary,-22}: {copy.Critical}={scanResult.CriticalCount}, {copy.High}={scanResult.HighCount}, {copy.Medium}={scanResult.MediumCount}, {copy.Low}={scanResult.LowCount}, {copy.Info}={scanResult.InfoCount}");
        sb.AppendLine();
        sb.AppendLine(Wrap(copy.Intro));
        sb.AppendLine();
        sb.AppendLine(copy.ContentNote);
        sb.AppendLine();
        sb.AppendLine(copy.ExecutiveHeading);
        sb.AppendLine("--------------------------------------------------------------------------------");
        sb.AppendLine(Wrap(scanResult.ExecutiveSummary ?? scanResult.Summary ?? copy.NoSummary));
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(scanResult.TechnicalSummary))
        {
            sb.AppendLine(copy.TechnicalHeading);
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine(Wrap(scanResult.TechnicalSummary));
            sb.AppendLine();
        }

        var bbCandidates = findings
            .Where(f => f.BugBountyEligible && f.DemonstratedImpact
                        && f.SubmissionRecommendation != Domain.Enums.SubmissionRecommendation.DoNotSubmit)
            .ToList();

        sb.AppendLine(copy.AssessmentSection);
        sb.AppendLine("--------------------------------------------------------------------------------");
        if (findings.Count == 0)
        {
            sb.AppendLine(copy.NoFindings);
        }
        else
        {
            var index = 1;
            foreach (var f in findings)
            {
                AppendFindingText(sb, copy, host, f, index++);
            }
        }

        sb.AppendLine();
        sb.AppendLine(copy.BugBountySection);
        sb.AppendLine("--------------------------------------------------------------------------------");
        sb.AppendLine("Policy: AmazonVRP — scanner severity is independent from bug bounty eligibility.");
        if (bbCandidates.Count == 0)
        {
            sb.AppendLine(copy.BugBountyEmpty);
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
            sb.AppendLine($"$$$  BUG BOUNTY MONEY SIGNAL — {bbCandidates.Count} CANDIDATE(S)  $$$");
            sb.AppendLine("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
            var index = 1;
            foreach (var f in bbCandidates)
            {
                AppendFindingText(sb, copy, host, f, index++, highlightBb: true);
            }
        }

        sb.AppendLine();
        sb.AppendLine(copy.ClosingHeading);
        sb.AppendLine("--------------------------------------------------------------------------------");
        sb.AppendLine(copy.GeneratedBy);
        sb.AppendLine(copy.ClosingBody);
        sb.AppendLine("================================================================================");

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var safeHost = string.Join("-", host.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var report = new ExportedReport(
            $"kaan-security-report-{safeHost}-{ReportLanguageParser.ToCode(lang)}-{scanResult.ScanJobId:N}.txt",
            "text/plain; charset=utf-8",
            bytes);
        return Task.FromResult(report);
    }

    private static void AppendFindingText(
        StringBuilder sb,
        ReportCopy copy,
        string host,
        Domain.Entities.Findings.Finding f,
        int index,
        bool highlightBb = false)
    {
        var tech = copy.SeverityLabel(f.TechnicalSeverity);
        var scan = copy.SeverityLabel(f.Severity);
        sb.AppendLine();
        if (highlightBb || f.BugBountyEligible)
        {
            sb.AppendLine($"--- $$$ {copy.FindingLabel} #{index} [BUG BOUNTY ADAYI] ---");
        }
        else
        {
            sb.AppendLine($"--- {copy.FindingLabel} #{index} ---");
        }
        sb.AppendLine($"{copy.Title,-22}: {f.Title}");
        sb.AppendLine($"{copy.TechnicalSeverityLabel,-22}: {tech}");
        sb.AppendLine($"{copy.ScannerSeverity,-22}: {scan}");
        sb.AppendLine($"{copy.FindingClassLabel,-22}: {f.FindingClass}");
        sb.AppendLine($"{copy.BbEligibleLabel,-22}: {f.BugBountyEligible}");
        sb.AppendLine($"{copy.SubmissionLabel,-22}: {f.SubmissionRecommendation}");
        sb.AppendLine($"{copy.Confidence,-22}: {f.ConfidenceLevel}");
        sb.AppendLine($"{copy.Category,-22}: {f.Category}");
        sb.AppendLine($"{"CWE",-22}: {f.CweCode ?? "—"}");
        sb.AppendLine($"{"OWASP",-22}: {f.OwaspCategory ?? "—"}");
        sb.AppendLine($"{copy.AffectedUrl,-22}: {f.AffectedUrl ?? "—"}");
        sb.AppendLine($"{copy.Parameter,-22}: {f.AffectedParameter ?? "—"}");
        sb.AppendLine($"{copy.CheckCode,-22}: {f.CheckCode}");
        sb.AppendLine($"{copy.Fingerprint,-22}: {f.Fingerprint ?? "—"}");
        sb.AppendLine($"{copy.Status,-22}: {f.Status}");
        if (!string.IsNullOrWhiteSpace(f.EligibilityReason))
        {
            sb.AppendLine();
            sb.AppendLine(Wrap(f.EligibilityReason));
        }
        sb.AppendLine();
        sb.AppendLine($"{copy.Description}:");
        sb.AppendLine(Wrap(f.Description));
        if (!string.IsNullOrWhiteSpace(f.Remediation))
        {
            sb.AppendLine();
            sb.AppendLine($"{copy.Remediation}:");
            sb.AppendLine(Wrap(f.Remediation));
        }
        if (f.BugBountyEligible)
        {
            sb.AppendLine();
            sb.AppendLine($"{copy.VendorSnippetHeading}:");
            sb.AppendLine(Wrap(copy.VendorRequest(host, f.Title, tech, f.CweCode, f.AffectedUrl, f.Remediation)));
        }
    }

    private static string Wrap(string text, int width = 88)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "—";
        }

        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = new List<string>();
        foreach (var paragraph in normalized.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                lines.Add(string.Empty);
                continue;
            }

            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = new StringBuilder();
            foreach (var word in words)
            {
                if (current.Length == 0)
                {
                    current.Append(word);
                }
                else if (current.Length + 1 + word.Length <= width)
                {
                    current.Append(' ').Append(word);
                }
                else
                {
                    lines.Add(current.ToString());
                    current.Clear().Append(word);
                }
            }
            if (current.Length > 0)
            {
                lines.Add(current.ToString());
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}
