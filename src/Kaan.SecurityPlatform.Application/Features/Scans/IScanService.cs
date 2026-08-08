using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Scans.Dtos;

namespace Kaan.SecurityPlatform.Application.Features.Scans;

public interface IScanService
{
    Task<Result<StartScanResponse>> StartAsync(StartScanRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScanJobListItemDto>> ListAsync(Guid? projectId = null, CancellationToken cancellationToken = default);
    Task<Result<ScanJobDetailDto>> GetAsync(Guid scanJobId, CancellationToken cancellationToken = default);
    Task<Result<ScanProgressDto>> GetProgressAsync(Guid scanJobId, CancellationToken cancellationToken = default);
    Task<Result<StartScanResponse>> RetestFindingAsync(RetestRequest request, CancellationToken cancellationToken = default);
}
