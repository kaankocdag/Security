using Kaan.SecurityPlatform.Domain.Entities.Scans;

namespace Kaan.SecurityPlatform.Application.Common.Interfaces;

public interface IReportExporter
{
    string Format { get; }
    Task<ExportedReport> ExportAsync(
        ScanResult scanResult,
        ReportExportOptions options,
        CancellationToken cancellationToken = default);
}

public sealed record ReportExportOptions(string LanguageCode = "tr");

public sealed record ExportedReport(
    string FileName,
    string ContentType,
    byte[] Content);
