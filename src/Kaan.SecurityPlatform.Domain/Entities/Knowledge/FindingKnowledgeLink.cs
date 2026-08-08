using Kaan.SecurityPlatform.Domain.Common;
using Kaan.SecurityPlatform.Domain.Entities.Findings;

namespace Kaan.SecurityPlatform.Domain.Entities.Knowledge;

public class FindingKnowledgeLink : BaseEntity
{
    public Guid FindingId { get; set; }
    public Guid ArticleId { get; set; }
    public int RelevanceScore { get; set; } = 100;
    public bool IsAutoLinked { get; set; }

    public Finding? Finding { get; set; }
    public KnowledgeArticle? Article { get; set; }
}
