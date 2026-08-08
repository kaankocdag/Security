using Kaan.SecurityPlatform.Application.Common.Interfaces;
using Kaan.SecurityPlatform.Application.Common.Models;
using Kaan.SecurityPlatform.Application.Features.Knowledge;
using Kaan.SecurityPlatform.Application.Features.Knowledge.Dtos;
using Kaan.SecurityPlatform.Domain.Entities.Knowledge;
using Markdig;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;

namespace Kaan.SecurityPlatform.Infrastructure.Knowledge;

public sealed class KnowledgeService : IKnowledgeService
{
    private readonly IApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly MarkdownPipeline _markdownPipeline;

    public KnowledgeService(
        IApplicationDbContext db,
        IFileStorage storage,
        ICurrentUser currentUser,
        IDateTimeProvider clock)
    {
        _db = db;
        _storage = storage;
        _currentUser = currentUser;
        _clock = clock;
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoLinks()
            .UseEmphasisExtras()
            .UsePipeTables()
            .DisableHtml()
            .Build();
    }

    public async Task<IReadOnlyList<KnowledgeCategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.KnowledgeCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new KnowledgeCategoryDto(
                c.Id, c.Slug, c.Name, c.Description, c.IconName,
                c.ParentCategoryId, c.DisplayOrder,
                c.Articles.Count(a => a.IsPublished)))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<KnowledgeCategoryDto>> UpsertCategoryAsync(Guid? id, UpsertKnowledgeCategoryRequest request, CancellationToken cancellationToken = default)
    {
        KnowledgeCategory? category = null;
        if (id is Guid cid)
        {
            category = await _db.KnowledgeCategories.FirstOrDefaultAsync(c => c.Id == cid, cancellationToken);
            if (category is null)
            {
                return Result<KnowledgeCategoryDto>.Failure("category_not_found", "Kategori bulunamadı.");
            }
        }
        else
        {
            var slugExists = await _db.KnowledgeCategories.AnyAsync(c => c.Slug == request.Slug, cancellationToken);
            if (slugExists)
            {
                return Result<KnowledgeCategoryDto>.Failure("slug_conflict", "Bu slug zaten kullanılıyor.");
            }
            category = new KnowledgeCategory();
            _db.KnowledgeCategories.Add(category);
        }

        category.Slug = request.Slug;
        category.Name = request.Name;
        category.Description = request.Description;
        category.IconName = request.IconName;
        category.ParentCategoryId = request.ParentCategoryId;
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = true;

        await _db.SaveChangesAsync(cancellationToken);
        return Result<KnowledgeCategoryDto>.Success(new KnowledgeCategoryDto(
            category.Id, category.Slug, category.Name, category.Description, category.IconName,
            category.ParentCategoryId, category.DisplayOrder, 0));
    }

    public async Task<Result> DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _db.KnowledgeCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (category is null)
        {
            return Result.Failure("category_not_found", "Kategori bulunamadı.");
        }

        var hasArticles = await _db.KnowledgeArticles.AnyAsync(a => a.CategoryId == id, cancellationToken);
        if (hasArticles)
        {
            category.IsActive = false;
        }
        else
        {
            _db.KnowledgeCategories.Remove(category);
        }
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<IReadOnlyList<KnowledgeArticleListItemDto>> ListArticlesAsync(
        Guid? categoryId = null,
        string? tag = null,
        bool includeUnpublished = false,
        CancellationToken cancellationToken = default)
    {
        var query = _db.KnowledgeArticles.AsQueryable();
        if (!includeUnpublished)
        {
            query = query.Where(a => a.IsPublished);
        }
        if (categoryId is Guid cid)
        {
            query = query.Where(a => a.CategoryId == cid);
        }
        if (!string.IsNullOrWhiteSpace(tag))
        {
            var normalizedTag = tag.Trim().ToLowerInvariant();
            query = query.Where(a => a.Tags != null && a.Tags.ToLower().Contains(normalizedTag));
        }

        return await query
            .OrderByDescending(a => a.IsFeatured)
            .ThenByDescending(a => a.PublishedAt)
            .Select(a => new KnowledgeArticleListItemDto(
                a.Id, a.Slug, a.Title, a.Summary,
                a.CategoryId, a.Category!.Slug, a.Category.Name,
                a.CweCode, a.OwaspCategory, a.DifficultyLevel,
                a.EstimatedReadMinutes,
                (a.Tags ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                a.CoverMediaAsset != null ? a.CoverMediaAsset.PublicUrl : null,
                a.PublishedAt,
                a.IsPublished,
                a.IsFeatured))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<KnowledgeArticleDetailDto>> GetArticleAsync(string slug, CancellationToken cancellationToken = default)
    {
        var article = await _db.KnowledgeArticles
            .Include(a => a.Category)
            .Include(a => a.MediaAssets)
            .Include(a => a.References)
            .FirstOrDefaultAsync(a => a.Slug == slug, cancellationToken);
        if (article is null)
        {
            return Result<KnowledgeArticleDetailDto>.Failure("article_not_found", "Makale bulunamadı.");
        }

        if (!article.IsPublished && !_currentUser.IsSystemAdmin)
        {
            return Result<KnowledgeArticleDetailDto>.Failure("article_not_found", "Makale bulunamadı.");
        }

        article.ViewCount++;
        await _db.SaveChangesAsync(cancellationToken);

        var html = Markdown.ToHtml(article.BodyMarkdown, _markdownPipeline);
        return Result<KnowledgeArticleDetailDto>.Success(new KnowledgeArticleDetailDto(
            article.Id,
            article.Slug,
            article.Title,
            article.Summary,
            article.BodyMarkdown,
            html,
            article.CategoryId,
            article.Category?.Slug ?? string.Empty,
            article.Category?.Name ?? string.Empty,
            article.CweCode,
            article.OwaspCategory,
            article.CveCode,
            article.DifficultyLevel,
            article.EstimatedReadMinutes,
            (article.Tags ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            article.SourceAttribution,
            article.SourceUrl,
            article.PublishedAt,
            article.MediaAssets
                .OrderBy(m => m.DisplayOrder)
                .Select(m => new KnowledgeMediaAssetDto(m.Id, m.PublicUrl ?? string.Empty, m.MimeType, m.Caption, m.AltText, m.DisplayOrder, m.Width, m.Height))
                .ToList(),
            article.References
                .OrderBy(r => r.DisplayOrder)
                .Select(r => new KnowledgeArticleReferenceDto(r.Id, r.ReferenceType, r.Url, r.Title, r.Description))
                .ToList(),
            article.IsPublished,
            article.IsFeatured));
    }

    public async Task<Result<KnowledgeArticleDetailDto>> UpsertArticleAsync(Guid? id, UpsertKnowledgeArticleRequest request, CancellationToken cancellationToken = default)
    {
        KnowledgeArticle? article = null;
        if (id is Guid aid)
        {
            article = await _db.KnowledgeArticles.FirstOrDefaultAsync(a => a.Id == aid, cancellationToken);
            if (article is null)
            {
                return Result<KnowledgeArticleDetailDto>.Failure("article_not_found", "Makale bulunamadı.");
            }
        }
        else
        {
            var slugExists = await _db.KnowledgeArticles.AnyAsync(a => a.Slug == request.Slug, cancellationToken);
            if (slugExists)
            {
                return Result<KnowledgeArticleDetailDto>.Failure("slug_conflict", "Bu slug zaten kullanılıyor.");
            }
            article = new KnowledgeArticle { AuthorUserId = _currentUser.UserId };
            _db.KnowledgeArticles.Add(article);
        }

        article.Slug = request.Slug;
        article.Title = request.Title;
        article.Summary = request.Summary;
        article.BodyMarkdown = request.BodyMarkdown;
        article.CategoryId = request.CategoryId;
        article.CweCode = request.CweCode;
        article.OwaspCategory = request.OwaspCategory;
        article.CveCode = request.CveCode;
        article.DifficultyLevel = request.DifficultyLevel;
        article.EstimatedReadMinutes = request.EstimatedReadMinutes;
        article.Tags = request.Tags;
        article.SourceAttribution = request.SourceAttribution;
        article.SourceUrl = request.SourceUrl;
        article.IsFeatured = request.IsFeatured;

        if (request.IsPublished && !article.IsPublished)
        {
            article.PublishedAt = _clock.UtcNow;
        }
        article.IsPublished = request.IsPublished;

        await _db.SaveChangesAsync(cancellationToken);
        return await GetArticleAsync(article.Slug, cancellationToken);
    }

    public async Task<Result> DeleteArticleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var article = await _db.KnowledgeArticles.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (article is null)
        {
            return Result.Failure("article_not_found", "Makale bulunamadı.");
        }

        _db.KnowledgeArticles.Remove(article);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<UploadMediaResponse>> UploadMediaAsync(
        Guid articleId,
        Stream content,
        string originalFileName,
        string contentType,
        string? caption,
        CancellationToken cancellationToken = default)
    {
        var article = await _db.KnowledgeArticles.FirstOrDefaultAsync(a => a.Id == articleId, cancellationToken);
        if (article is null)
        {
            return Result<UploadMediaResponse>.Failure("article_not_found", "Makale bulunamadı.");
        }

        var subFolder = $"knowledge/{_clock.UtcNow:yyyy}/{_clock.UtcNow:MM}";
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        int? width = null;
        int? height = null;
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                memory.Position = 0;
                using var image = await Image.LoadAsync(memory, cancellationToken);
                width = image.Width;
                height = image.Height;
            }
            catch
            {
            }
            memory.Position = 0;
        }

        var stored = await _storage.SaveAsync(memory, originalFileName, contentType, subFolder, cancellationToken);

        var asset = new KnowledgeMediaAsset
        {
            ArticleId = articleId,
            StoragePath = stored.StoragePath,
            PublicUrl = stored.PublicUrl,
            MimeType = contentType,
            OriginalFileName = originalFileName,
            FileSizeBytes = stored.SizeBytes,
            Sha256Hash = stored.Sha256Hash,
            Caption = caption,
            AltText = caption,
            Width = width,
            Height = height,
            DisplayOrder = await _db.KnowledgeMediaAssets.CountAsync(m => m.ArticleId == articleId, cancellationToken),
            CreatedAt = _clock.UtcNow
        };

        _db.KnowledgeMediaAssets.Add(asset);

        if (article.CoverMediaAssetId is null)
        {
            article.CoverMediaAssetId = asset.Id;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result<UploadMediaResponse>.Success(new UploadMediaResponse(
            asset.Id,
            asset.PublicUrl ?? string.Empty,
            asset.StoragePath,
            asset.FileSizeBytes,
            asset.MimeType,
            asset.Width,
            asset.Height));
    }

    public async Task<Result> DeleteMediaAsync(Guid mediaId, CancellationToken cancellationToken = default)
    {
        var media = await _db.KnowledgeMediaAssets.FirstOrDefaultAsync(m => m.Id == mediaId, cancellationToken);
        if (media is null)
        {
            return Result.Failure("media_not_found", "Medya bulunamadı.");
        }

        await _storage.DeleteAsync(media.StoragePath, cancellationToken);
        _db.KnowledgeMediaAssets.Remove(media);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> LinkFindingToArticleAsync(Guid findingId, LinkFindingToArticleRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _db.FindingKnowledgeLinks.AnyAsync(l => l.FindingId == findingId && l.ArticleId == request.ArticleId, cancellationToken);
        if (exists)
        {
            return Result.Success();
        }

        _db.FindingKnowledgeLinks.Add(new FindingKnowledgeLink
        {
            FindingId = findingId,
            ArticleId = request.ArticleId,
            RelevanceScore = request.RelevanceScore
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> UnlinkFindingAsync(Guid findingId, Guid articleId, CancellationToken cancellationToken = default)
    {
        var link = await _db.FindingKnowledgeLinks.FirstOrDefaultAsync(l => l.FindingId == findingId && l.ArticleId == articleId, cancellationToken);
        if (link is not null)
        {
            _db.FindingKnowledgeLinks.Remove(link);
            await _db.SaveChangesAsync(cancellationToken);
        }
        return Result.Success();
    }
}
