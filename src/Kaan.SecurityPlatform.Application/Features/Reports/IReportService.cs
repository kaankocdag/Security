using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;

namespace Kaan.SecurityPlatform.Application.Features.Reports;

public interface IReportService
{
    Task<Result<ExportedReport>> ExportAsync(
        Guid scanJobId,
        string format = "html",
        string language = "tr",
        CancellationToken cancellationToken = default);
}
