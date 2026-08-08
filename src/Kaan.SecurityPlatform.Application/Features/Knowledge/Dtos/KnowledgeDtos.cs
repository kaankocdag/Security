using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Application.Features.Knowledge.Dtos;

public sealed record KnowledgeCategoryDto(
    Guid Id,
    string Slug,
    string Name,
    string? Description,
    string? IconName,
    Guid? ParentCategoryId,
    int DisplayOrder,
    int PublishedArticleCount);

public sealed record UpsertKnowledgeCategoryRequest(
    string Slug,
    string Name,
    string? Description,
    string? IconName,
    Guid? ParentCategoryId,
    int DisplayOrder);

public sealed record KnowledgeArticleListItemDto(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    Guid CategoryId,
    string CategorySlug,
    string CategoryName,
    string? CweCode,
    string? OwaspCategory,
    DifficultyLevel DifficultyLevel,
    int EstimatedReadMinutes,
    IReadOnlyList<string> Tags,
    string? CoverMediaUrl,
    DateTime? PublishedAt,
    bool IsPublished,
    bool IsFeatured);

public sealed record KnowledgeArticleDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string Summary,
    string BodyMarkdown,
    string BodyHtml,
    Guid CategoryId,
    string CategorySlug,
    string CategoryName,
    string? CweCode,
    string? OwaspCategory,
    string? CveCode,
    DifficultyLevel DifficultyLevel,
    int EstimatedReadMinutes,
    IReadOnlyList<string> Tags,
    string? SourceAttribution,
    string? SourceUrl,
    DateTime? PublishedAt,
    IReadOnlyList<KnowledgeMediaAssetDto> MediaAssets,
    IReadOnlyList<KnowledgeArticleReferenceDto> References,
    bool IsPublished,
    bool IsFeatured);

public sealed record KnowledgeMediaAssetDto(
    Guid Id,
    string PublicUrl,
    string MimeType,
    string? Caption,
    string? AltText,
    int DisplayOrder,
    int? Width,
    int? Height);

public sealed record KnowledgeArticleReferenceDto(
    Guid Id,
    KnowledgeReferenceType ReferenceType,
    string Url,
    string Title,
    string? Description);

public sealed record UpsertKnowledgeArticleRequest(
    string Slug,
    string Title,
    string Summary,
    string BodyMarkdown,
    Guid CategoryId,
    string? CweCode,
    string? OwaspCategory,
    string? CveCode,
    DifficultyLevel DifficultyLevel,
    int EstimatedReadMinutes,
    string? Tags,
    string? SourceAttribution,
    string? SourceUrl,
    bool IsPublished,
    bool IsFeatured);

public sealed record UploadMediaResponse(
    Guid Id,
    string PublicUrl,
    string StoragePath,
    long SizeBytes,
    string MimeType,
    int? Width,
    int? Height);

public sealed record LinkFindingToArticleRequest(Guid ArticleId, int RelevanceScore = 100);
