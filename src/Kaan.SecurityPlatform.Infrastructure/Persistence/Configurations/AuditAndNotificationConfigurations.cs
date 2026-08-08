using Kaan.SecurityPlatform.Domain.Entities.Audit;
using Kaan.SecurityPlatform.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserEmail).HasMaxLength(320);
        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(64);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.Category).HasMaxLength(64);
        builder.Property(x => x.Details).HasMaxLength(4000);

        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasIndex(x => new { x.CompanyId, x.CreatedAt });
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
    }
}

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.ActionUrl).HasMaxLength(1024);
        builder.Property(x => x.Icon).HasMaxLength(64);
        builder.Property(x => x.RelatedEntityType).HasMaxLength(128);

        builder.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        builder.HasIndex(x => new { x.CompanyId, x.CreatedAt });
    }
}
