using Kaan.SecurityPlatform.Domain.Entities.Scans;
using Kaan.SecurityPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Configurations;

public sealed class ScanJobConfiguration : IEntityTypeConfiguration<ScanJob>
{
    public void Configure(EntityTypeBuilder<ScanJob> builder)
    {
        builder.ToTable("ScanJobs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CurrentStep).HasMaxLength(256);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2048);
        builder.Property(x => x.ScannerVersion).HasMaxLength(32);
        builder.Property(x => x.HangfireJobId).HasMaxLength(64);
        builder.Property(x => x.AssessmentMode)
            .HasDefaultValue(AssessmentMode.PublicPassiveAssessment);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.AssessmentMode);
        builder.HasIndex(x => new { x.SecurityProjectId, x.Status });
        builder.HasIndex(x => new { x.DomainAssetId, x.CreatedAt });
        builder.HasIndex(x => x.HangfireJobId);

        builder.HasOne(x => x.SecurityProject)
            .WithMany(p => p.ScanJobs)
            .HasForeignKey(x => x.SecurityProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DomainAsset)
            .WithMany(d => d.ScanJobs)
            .HasForeignKey(x => x.DomainAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PreviousScanJob)
            .WithMany()
            .HasForeignKey(x => x.PreviousScanJobId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ScanResultConfiguration : IEntityTypeConfiguration<ScanResult>
{
    public void Configure(EntityTypeBuilder<ScanResult> builder)
    {
        builder.ToTable("ScanResults");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Summary).HasMaxLength(4000);
        builder.Property(x => x.ExecutiveSummary).HasMaxLength(4000);
        builder.Property(x => x.TechnicalSummary).HasMaxLength(4000);

        builder.HasIndex(x => x.ScanJobId).IsUnique();

        builder.HasOne(x => x.ScanJob)
            .WithOne(j => j.Result)
            .HasForeignKey<ScanResult>(x => x.ScanJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
