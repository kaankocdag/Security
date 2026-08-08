using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Projects.Dtos;

namespace Kaan.SecurityPlatform.Application.Features.Projects;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<Result<ProjectDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<ProjectDetailDto>> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);
    Task<Result<ProjectDetailDto>> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken = default);
    Task<Result> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}
