using System.Globalization;
using System.Net;
using System.Text;
using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Features.Reports;
using Kaan.SecurityPlatform.Domain.Entities.Scans;

namespace Kaan.SecurityPlatform.Infrastructure.Reporting;

public sealed class HtmlReportExporter : IReportExporter
{
    public string Format => "html";

    public Task<ExportedReport> ExportAsync(
        ScanResult scanResult,
        ReportExportOptions options,
        CancellationToken cancellationToken = default)
    {
        var lang = ReportLanguageParser.Parse(options.LanguageCode);
        var copy = ReportCopy.For(lang);
        var culture = CultureInfo.GetCultureInfo(lang == ReportLanguage.En ? "en-US" : "tr-TR");
        var host = scanResult.ScanJob?.DomainAsset?.HostName
            ?? scanResult.ScanJob?.DomainAsset?.NormalizedHostName
            ?? "—";

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine($"<html lang=\"{copy.LanguageTag}\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\" />");
        sb.AppendLine($"<title>{WebUtility.HtmlEncode(copy.DocumentTitle)}</title>");
        sb.AppendLine("<style>body{font-family:'Segoe UI',sans-serif;color:#0f172a;background:#f8fafc;padding:32px;max-width:960px;margin:0 auto;} h1{color:#1e40af;} .score{font-size:48px;font-weight:700;} table{width:100%;border-collapse:collapse;margin-top:24px;} td,th{border:1px solid #e2e8f0;padding:8px;text-align:left;} th{background:#eff6ff;} .meta{color:#475569;font-size:14px;} .finding{border-left:4px solid #1e40af;padding:12px;margin:16px 0;background:white;} .finding.bb{border-left:6px solid #059669;background:#ecfdf5;box-shadow:0 0 0 2px #6ee7b7;} .bb-banner{display:flex;align-items:center;gap:12px;background:linear-gradient(90deg,#059669,#10b981);color:white;padding:14px 18px;border-radius:12px;margin:20px 0;font-weight:700;} .bb-badge{display:inline-block;background:#059669;color:#fff;font-weight:800;font-size:12px;padding:2px 8px;border-radius:999px;margin-left:8px;} .note{font-size:12px;color:#64748b;margin-top:8px;}</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine($"<h1>{WebUtility.HtmlEncode(copy.DocumentTitle)}</h1>");
        sb.AppendLine($"<p class=\"meta\">{WebUtility.HtmlEncode(copy.TargetDomain)}: <strong>{WebUtility.HtmlEncode(host)}</strong></p>");
        sb.AppendLine($"<p class=\"meta\">{WebUtility.HtmlEncode(copy.ReportDate)}: {scanResult.CompletedAt.ToString("dd MMMM yyyy HH:mm", culture)} UTC · {WebUtility.HtmlEncode(copy.LanguageTag.ToUpperInvariant())}</p>");
        sb.AppendLine($"<div class=\"score\">{WebUtility.HtmlEncode(copy.ScoreLabel)}: {scanResult.SecurityScore}/100</div>");
        sb.AppendLine($"<p class=\"note\">{WebUtility.HtmlEncode(copy.ContentNote)}</p>");
        sb.AppendLine($"<h2>{WebUtility.HtmlEncode(copy.SummaryHeading)}</h2>");
        sb.AppendLine($"<p>{WebUtility.HtmlEncode(scanResult.ExecutiveSummary ?? scanResult.Summary ?? copy.NoSummary)}</p>");
        sb.AppendLine("<table>");
        sb.AppendLine($"<tr><th>{WebUtility.HtmlEncode(copy.CategoryCol)}</th><th>{WebUtility.HtmlEncode(copy.CountCol)}</th></tr>");
        sb.AppendLine($"<tr><td>{WebUtility.HtmlEncode(copy.Critical)}</td><td>{scanResult.CriticalCount}</td></tr>");
        sb.AppendLine($"<tr><td>{WebUtility.HtmlEncode(copy.High)}</td><td>{scanResult.HighCount}</td></tr>");
        sb.AppendLine($"<tr><td>{WebUtility.HtmlEncode(copy.Medium)}</td><td>{scanResult.MediumCount}</td></tr>");
        sb.AppendLine($"<tr><td>{WebUtility.HtmlEncode(copy.Low)}</td><td>{scanResult.LowCount}</td></tr>");
        sb.AppendLine($"<tr><td>{WebUtility.HtmlEncode(copy.Info)}</td><td>{scanResult.InfoCount}</td></tr>");
        sb.AppendLine("</table>");

        var all = scanResult.Findings.OrderByDescending(f => f.TechnicalSeverity).ToList();
        var bbCandidates = all
            .Where(f => f.BugBountyEligible && f.DemonstratedImpact
                        && f.SubmissionRecommendation != Domain.Enums.SubmissionRecommendation.DoNotSubmit)
            .ToList();

        sb.AppendLine($"<h2>{WebUtility.HtmlEncode(copy.AssessmentSection)}</h2>");
        if (all.Count == 0)
        {
            sb.AppendLine($"<p>{WebUtility.HtmlEncode(copy.NoFindings)}</p>");
        }
        else
        {
            foreach (var finding in all)
            {
                AppendFindingHtml(sb, copy, finding, useTechnical: true);
            }
        }

        sb.AppendLine($"<h2>{WebUtility.HtmlEncode(copy.BugBountySection)}</h2>");
        if (bbCandidates.Count == 0)
        {
            sb.AppendLine($"<p class=\"note\">Policy: AmazonVRP — scanner severity ≠ bug bounty severity.</p>");
            sb.AppendLine($"<p>{WebUtility.HtmlEncode(copy.BugBountyEmpty)}</p>");
        }
        else
        {
            sb.AppendLine("<div class=\"bb-banner\">");
            sb.AppendLine($"<span style=\"font-size:28px\">$$$</span>");
            sb.AppendLine(lang == ReportLanguage.En
                ? $"<div><div>{bbCandidates.Count} BUG BOUNTY CANDIDATE(S)</div><div style=\"font-weight:500;font-size:13px;opacity:.95\">Money-signal: demonstrated impact + AmazonVRP policy match</div></div>"
                : $"<div><div>{bbCandidates.Count} BUG BOUNTY ADAYI — PARA SİNYALİ</div><div style=\"font-weight:500;font-size:13px;opacity:.95\">Demonstrated impact + AmazonVRP politika uygunluğu</div></div>");
            sb.AppendLine("</div>");
            foreach (var finding in bbCandidates)
            {
                AppendFindingHtml(sb, copy, finding, useTechnical: true, highlightBb: true);
            }
        }

        sb.AppendLine($"<p class=\"note\">{WebUtility.HtmlEncode(copy.GeneratedBy)}</p>");
        sb.AppendLine("</body></html>");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var safeHost = string.Join("-", host.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(safeHost) || safeHost == "—")
        {
            safeHost = "report";
        }

        var report = new ExportedReport(
            $"kaan-security-report-{safeHost}-{ReportLanguageParser.ToCode(lang)}-{scanResult.ScanJobId:N}.html",
            "text/html; charset=utf-8",
            bytes);
        return Task.FromResult(report);
    }

    private static void AppendFindingHtml(
        StringBuilder sb,
        ReportCopy copy,
        Domain.Entities.Findings.Finding finding,
        bool useTechnical,
        bool highlightBb = false)
    {
        var tech = copy.SeverityLabel(finding.TechnicalSeverity);
        var scan = copy.SeverityLabel(finding.Severity);
        var isBb = highlightBb || finding.BugBountyEligible;
        sb.AppendLine(isBb ? "<div class=\"finding bb\">" : "<div class=\"finding\">");
        sb.AppendLine(
            isBb
                ? $"<h3>{WebUtility.HtmlEncode(finding.Title)} <span class=\"bb-badge\">$$$ BB</span></h3>"
                : $"<h3>{WebUtility.HtmlEncode(finding.Title)}</h3>");
        sb.AppendLine(
            $"<p><strong>{WebUtility.HtmlEncode(copy.TechnicalSeverityLabel)}:</strong> {WebUtility.HtmlEncode(tech)} · " +
            $"<strong>{WebUtility.HtmlEncode(copy.ScannerSeverity)}:</strong> {WebUtility.HtmlEncode(scan)} · " +
            $"<strong>{WebUtility.HtmlEncode(copy.FindingClassLabel)}:</strong> {finding.FindingClass} · " +
            $"<strong>{WebUtility.HtmlEncode(copy.BbEligibleLabel)}:</strong> {finding.BugBountyEligible} · " +
            $"<strong>{WebUtility.HtmlEncode(copy.SubmissionLabel)}:</strong> {finding.SubmissionRecommendation}</p>");
        sb.AppendLine($"<p>{WebUtility.HtmlEncode(finding.Description)}</p>");
        if (!string.IsNullOrWhiteSpace(finding.EligibilityReason))
        {
            sb.AppendLine($"<p class=\"note\">{WebUtility.HtmlEncode(finding.EligibilityReason)}</p>");
        }
        if (!string.IsNullOrWhiteSpace(finding.Remediation))
        {
            sb.AppendLine($"<h4>{WebUtility.HtmlEncode(copy.RemediationHtmlHeading)}</h4>");
            sb.AppendLine($"<pre>{WebUtility.HtmlEncode(finding.Remediation)}</pre>");
        }
        sb.AppendLine("</div>");
    }
}
