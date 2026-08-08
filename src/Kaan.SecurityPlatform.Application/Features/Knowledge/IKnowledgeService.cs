using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Knowledge.Dtos;

namespace Kaan.SecurityPlatform.Application.Features.Knowledge;

public interface IKnowledgeService
{
    Task<IReadOnlyList<KnowledgeCategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default);
    Task<Result<KnowledgeCategoryDto>> UpsertCategoryAsync(Guid? id, UpsertKnowledgeCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeArticleListItemDto>> ListArticlesAsync(Guid? categoryId = null, string? tag = null, bool includeUnpublished = false, CancellationToken cancellationToken = default);
    Task<Result<KnowledgeArticleDetailDto>> GetArticleAsync(string slug, CancellationToken cancellationToken = default);
    Task<Result<KnowledgeArticleDetailDto>> UpsertArticleAsync(Guid? id, UpsertKnowledgeArticleRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteArticleAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<UploadMediaResponse>> UploadMediaAsync(Guid articleId, Stream content, string originalFileName, string contentType, string? caption, CancellationToken cancellationToken = default);
    Task<Result> DeleteMediaAsync(Guid mediaId, CancellationToken cancellationToken = default);

    Task<Result> LinkFindingToArticleAsync(Guid findingId, LinkFindingToArticleRequest request, CancellationToken cancellationToken = default);
    Task<Result> UnlinkFindingAsync(Guid findingId, Guid articleId, CancellationToken cancellationToken = default);
}
