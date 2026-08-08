using Kaan.SecurityPlatform.Application.Authorization;
using Kaan.SecurityPlatform.Application.Features.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kaan.SecurityPlatform.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = PolicyNames.RequireApprovedMember)]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    /// <summary>format=html|txt, lang=tr|en</summary>
    [HttpGet("{scanJobId:guid}")]
    public async Task<IActionResult> Get(
        Guid scanJobId,
        [FromQuery] string format = "html",
        [FromQuery] string lang = "tr",
        CancellationToken cancellationToken = default)
    {
        var result = await _reports.ExportAsync(scanJobId, format, lang, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(title: result.ErrorCode, detail: result.ErrorMessage, statusCode: StatusCodes.Status400BadRequest);
        }
        var report = result.Value!;
        return File(report.Content, report.ContentType, report.FileName);
    }
}
