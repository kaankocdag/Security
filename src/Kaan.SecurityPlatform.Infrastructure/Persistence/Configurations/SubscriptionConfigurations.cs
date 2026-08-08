using Kaan.SecurityPlatform.Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        builder.Property(x => x.MonthlyPrice).HasPrecision(18, 2);

        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class CompanySubscriptionConfiguration : IEntityTypeConfiguration<CompanySubscription>
{
    public void Configure(EntityTypeBuilder<CompanySubscription> builder)
    {
        builder.ToTable("CompanySubscriptions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasMaxLength(32);
        builder.Property(x => x.Notes).HasMaxLength(1024);

        builder.HasIndex(x => new { x.CompanyId, x.Status });

        builder.HasOne(x => x.Company)
            .WithMany(c => c.Subscriptions)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SubscriptionPlan)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(x => x.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
