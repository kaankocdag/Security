using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Knowledge;

public class KnowledgeArticle : BaseEntity, IAuditableEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string BodyMarkdown { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string? CweCode { get; set; }
    public string? OwaspCategory { get; set; }
    public string? CveCode { get; set; }
    public DifficultyLevel DifficultyLevel { get; set; } = DifficultyLevel.Beginner;
    public int EstimatedReadMinutes { get; set; }
    public string? Tags { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? AuthorUserId { get; set; }
    public string? SourceAttribution { get; set; }
    public string? SourceUrl { get; set; }
    public int ViewCount { get; set; }
    public Guid? CoverMediaAssetId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public KnowledgeCategory? Category { get; set; }
    public KnowledgeMediaAsset? CoverMediaAsset { get; set; }
    public ICollection<KnowledgeMediaAsset> MediaAssets { get; set; } = new List<KnowledgeMediaAsset>();
    public ICollection<KnowledgeArticleReference> References { get; set; } = new List<KnowledgeArticleReference>();
    public ICollection<FindingKnowledgeLink> FindingLinks { get; set; } = new List<FindingKnowledgeLink>();
}
