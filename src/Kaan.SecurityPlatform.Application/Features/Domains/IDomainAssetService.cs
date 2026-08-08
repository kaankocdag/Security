using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Domains.Dtos;

namespace Kaan.SecurityPlatform.Application.Features.Domains;

public interface IDomainAssetService
{
    Task<IReadOnlyList<DomainListItemDto>> ListAsync(Guid? projectId = null, CancellationToken cancellationToken = default);
    Task<Result<DomainDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<DomainDetailDto>> CreateAsync(CreateDomainRequest request, CancellationToken cancellationToken = default);
    Task<Result<StartVerificationResponse>> StartVerificationAsync(StartVerificationRequest request, CancellationToken cancellationToken = default);
    Task<Result<RunVerificationResponse>> RunVerificationAsync(Guid domainId, CancellationToken cancellationToken = default);
    Task<Result<SetVerificationManualResponse>> SetVerificationManualAsync(SetVerificationManualRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
