using Kaan.SecurityPlatform.Domain.Common;

namespace Kaan.SecurityPlatform.Domain.Entities.Knowledge;

public class KnowledgeCategory : BaseEntity, IAuditableEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconName { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public KnowledgeCategory? ParentCategory { get; set; }
    public ICollection<KnowledgeCategory> Children { get; set; } = new List<KnowledgeCategory>();
    public ICollection<KnowledgeArticle> Articles { get; set; } = new List<KnowledgeArticle>();
}
