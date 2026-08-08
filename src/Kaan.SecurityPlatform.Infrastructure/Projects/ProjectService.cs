using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Projects;
using Kaan.SecurityPlatform.Application.Features.Projects.Dtos;
using Kaan.SecurityPlatform.Domain.Entities.Projects;
using Kaan.SecurityPlatform.Domain.Enums;
using Kaan.SecurityPlatform.Infrastructure.Common;
using Microsoft.EntityFrameworkCore;

namespace Kaan.SecurityPlatform.Infrastructure.Projects;

public sealed class ProjectService : IProjectService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;

    public ProjectService(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ProjectListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SecurityProjects
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProjectListItemDto(
                p.Id,
                p.Name,
                p.Description,
                p.EnvironmentType,
                p.Status,
                p.Domains.Count,
                p.ScanJobs
                    .Where(j => j.Result != null)
                    .SelectMany(j => j.Result!.Findings)
                    .Count(f => f.Status == FindingStatus.Open),
                p.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<ProjectDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _db.SecurityProjects
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return Result<ProjectDetailDto>.Failure("project_not_found", "Proje bulunamadı.");
        }

        return Result<ProjectDetailDto>.Success(Map(project));
    }

    public async Task<Result<ProjectDetailDto>> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var companyId = await CompanyIdResolver.ResolveAsync(_currentUser, _db, cancellationToken);
        var project = new SecurityProject
        {
            CompanyId = companyId,
            Name = request.Name,
            Description = request.Description,
            EnvironmentType = request.EnvironmentType,
            Status = ProjectStatus.Active,
            PrimaryContactEmail = request.PrimaryContactEmail,
            CreatedAt = _clock.UtcNow
        };

        _db.SecurityProjects.Add(project);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<ProjectDetailDto>.Success(Map(project));
    }

    public async Task<Result<ProjectDetailDto>> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = await _db.SecurityProjects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return Result<ProjectDetailDto>.Failure("project_not_found", "Proje bulunamadı.");
        }

        project.Name = request.Name;
        project.Description = request.Description;
        project.EnvironmentType = request.EnvironmentType;
        project.Status = request.Status;
        project.PrimaryContactEmail = request.PrimaryContactEmail;
        await _db.SaveChangesAsync(cancellationToken);

        return Result<ProjectDetailDto>.Success(Map(project));
    }

    public async Task<Result> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _db.SecurityProjects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return Result.Failure("project_not_found", "Proje bulunamadı.");
        }

        project.Status = ProjectStatus.Archived;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static ProjectDetailDto Map(SecurityProject project) => new(
        project.Id,
        project.CompanyId,
        project.Name,
        project.Description,
        project.EnvironmentType,
        project.Status,
        project.PrimaryContactEmail,
        project.CreatedAt,
        project.UpdatedAt);
}
