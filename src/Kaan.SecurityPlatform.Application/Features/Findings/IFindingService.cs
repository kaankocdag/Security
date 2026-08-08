using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Findings.Dtos;

namespace Kaan.SecurityPlatform.Application.Features.Findings;

public interface IFindingService
{
    Task<IReadOnlyList<FindingListItemDto>> ListAsync(Guid? scanResultId = null, Guid? projectId = null, CancellationToken cancellationToken = default);
    Task<Result<FindingDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> UpdateStatusAsync(Guid id, UpdateFindingStatusRequest request, CancellationToken cancellationToken = default);
}
