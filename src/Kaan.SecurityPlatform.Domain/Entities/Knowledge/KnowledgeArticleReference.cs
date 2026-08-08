using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Enums;

namespace Kaan.SecurityPlatform.Domain.Entities.Knowledge;

public class KnowledgeArticleReference : BaseEntity
{
    public Guid ArticleId { get; set; }
    public KnowledgeReferenceType ReferenceType { get; set; } = KnowledgeReferenceType.ExternalArticle;
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    public KnowledgeArticle? Article { get; set; }
}
