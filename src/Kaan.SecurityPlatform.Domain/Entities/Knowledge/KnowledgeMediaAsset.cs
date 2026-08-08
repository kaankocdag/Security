using Kaan.SecurityPlatform.Domain.Common;

namespace Kaan.SecurityPlatform.Domain.Entities.Knowledge;

public class KnowledgeMediaAsset : BaseEntity, IAuditableEntity
{
    public Guid? ArticleId { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? PublicUrl { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public string? Sha256Hash { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public KnowledgeArticle? Article { get; set; }
}
