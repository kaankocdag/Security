using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.BugBounty;
using Kaan.SecurityPlatform.Application.Features.Reports;
using Kaan.SecurityPlatform.Infrastructure.BugBounty;
using Microsoft.EntityFrameworkCore;

namespace Kaan.SecurityPlatform.Infrastructure.Reporting;

public sealed class ReportService : IReportService
{
    private readonly IApplicationDbContext _db;
    private readonly IEnumerable<IReportExporter> _exporters;
    private readonly IFindingValidationClassifier _classifier;

    public ReportService(
        IApplicationDbContext db,
        IEnumerable<IReportExporter> exporters,
        IFindingValidationClassifier classifier)
    {
        _db = db;
        _exporters = exporters;
        _classifier = classifier;
    }

    public async Task<Result<ExportedReport>> ExportAsync(
        Guid scanJobId,
        string format = "html",
        string language = "tr",
        CancellationToken cancellationToken = default)
    {
        var exporter = _exporters.FirstOrDefault(e => string.Equals(e.Format, format, StringComparison.OrdinalIgnoreCase));
        if (exporter is null)
        {
            return Result<ExportedReport>.Failure(
                "format_not_supported",
                $"'{format}' formatı desteklenmiyor. Desteklenenler: html, txt");
        }

        var job = await _db.ScanJobs
            .AsNoTracking()
            .Include(j => j.DomainAsset)
            .Include(j => j.Result!).ThenInclude(r => r.Findings)
            .FirstOrDefaultAsync(j => j.Id == scanJobId, cancellationToken);

        if (job?.Result is null)
        {
            return Result<ExportedReport>.Failure(
                "scan_not_completed",
                "Tarama tamamlanmadığı için rapor hazırlanamıyor.");
        }

        job.Result.ScanJob = job;

        // Eski bulgular için validation katmanını rapor anında tamamla
        foreach (var finding in job.Result.Findings)
        {
            if (string.IsNullOrWhiteSpace(finding.EligibilityReason))
            {
                _classifier.Classify(finding, AmazonVrpPolicy.PolicyKeyConstant);
            }
        }

        var options = new ReportExportOptions(ReportLanguageParser.ToCode(ReportLanguageParser.Parse(language)));
        var exported = await exporter.ExportAsync(job.Result, options, cancellationToken);
        return Result<ExportedReport>.Success(exported);
    }
}
