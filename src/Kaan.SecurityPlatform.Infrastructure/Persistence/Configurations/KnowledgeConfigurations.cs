using Kaan.SecurityPlatform.Domain.Entities.Knowledge;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Configurations;

public sealed class KnowledgeCategoryConfiguration : IEntityTypeConfiguration<KnowledgeCategory>
{
    public void Configure(EntityTypeBuilder<KnowledgeCategory> builder)
    {
        builder.ToTable("KnowledgeCategories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Slug).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2048);
        builder.Property(x => x.IconName).HasMaxLength(64);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.ParentCategoryId, x.DisplayOrder });

        builder.HasOne(x => x.ParentCategory)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class KnowledgeArticleConfiguration : IEntityTypeConfiguration<KnowledgeArticle>
{
    public void Configure(EntityTypeBuilder<KnowledgeArticle> builder)
    {
        builder.ToTable("KnowledgeArticles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Slug).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.BodyMarkdown).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.CweCode).HasMaxLength(32);
        builder.Property(x => x.OwaspCategory).HasMaxLength(128);
        builder.Property(x => x.CveCode).HasMaxLength(32);
        builder.Property(x => x.Tags).HasMaxLength(1024);
        builder.Property(x => x.SourceAttribution).HasMaxLength(256);
        builder.Property(x => x.SourceUrl).HasMaxLength(1024);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.CategoryId, x.IsPublished });
        builder.HasIndex(x => x.CweCode);
        builder.HasIndex(x => x.IsFeatured);

        builder.HasOne(x => x.Category)
            .WithMany(c => c.Articles)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // SQL Server çoklu cascade yolu kabul etmez (Article <-> MediaAsset).
        builder.HasOne(x => x.CoverMediaAsset)
            .WithMany()
            .HasForeignKey(x => x.CoverMediaAssetId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class KnowledgeMediaAssetConfiguration : IEntityTypeConfiguration<KnowledgeMediaAsset>
{
    public void Configure(EntityTypeBuilder<KnowledgeMediaAsset> builder)
    {
        builder.ToTable("KnowledgeMediaAssets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StoragePath).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.PublicUrl).HasMaxLength(1024);
        builder.Property(x => x.MimeType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Caption).HasMaxLength(512);
        builder.Property(x => x.AltText).HasMaxLength(256);
        builder.Property(x => x.Sha256Hash).HasMaxLength(96);

        builder.HasIndex(x => x.Sha256Hash);
        builder.HasIndex(x => x.ArticleId);

        builder.HasOne(x => x.Article)
            .WithMany(a => a.MediaAssets)
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class KnowledgeArticleReferenceConfiguration : IEntityTypeConfiguration<KnowledgeArticleReference>
{
    public void Configure(EntityTypeBuilder<KnowledgeArticleReference> builder)
    {
        builder.ToTable("KnowledgeArticleReferences");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Url).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1024);

        builder.HasOne(x => x.Article)
            .WithMany(a => a.References)
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FindingKnowledgeLinkConfiguration : IEntityTypeConfiguration<FindingKnowledgeLink>
{
    public void Configure(EntityTypeBuilder<FindingKnowledgeLink> builder)
    {
        builder.ToTable("FindingKnowledgeLinks");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.FindingId, x.ArticleId }).IsUnique();

        builder.HasOne(x => x.Finding)
            .WithMany(f => f.KnowledgeLinks)
            .HasForeignKey(x => x.FindingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Article)
            .WithMany(a => a.FindingLinks)
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
