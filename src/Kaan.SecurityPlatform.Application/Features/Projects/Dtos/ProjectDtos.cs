using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Features.Projects.Dtos;

public sealed record ProjectListItemDto(
    Guid Id,
    string Name,
    string? Description,
    EnvironmentType EnvironmentType,
    ProjectStatus Status,
    int DomainCount,
    int OpenFindingCount,
    DateTime CreatedAt);

public sealed record ProjectDetailDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string? Description,
    EnvironmentType EnvironmentType,
    ProjectStatus Status,
    string? PrimaryContactEmail,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record CreateProjectRequest(
    string Name,
    string? Description,
    EnvironmentType EnvironmentType,
    string? PrimaryContactEmail);

public sealed record UpdateProjectRequest(
    string Name,
    string? Description,
    EnvironmentType EnvironmentType,
    ProjectStatus Status,
    string? PrimaryContactEmail);
