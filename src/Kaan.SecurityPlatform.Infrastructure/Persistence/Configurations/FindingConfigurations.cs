using Kaan.SecurityPlatform.Domain.Entities.Findings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Configurations;

public sealed class FindingConfiguration : IEntityTypeConfiguration<Finding>
{
    public void Configure(EntityTypeBuilder<Finding> builder)
    {
        builder.ToTable("Findings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.TechnicalDescription).HasMaxLength(4000);
        builder.Property(x => x.BusinessImpact).HasMaxLength(2048);
        builder.Property(x => x.Category).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CweCode).HasMaxLength(32);
        builder.Property(x => x.OwaspCategory).HasMaxLength(128);
        builder.Property(x => x.AffectedUrl).HasMaxLength(2048);
        builder.Property(x => x.AffectedParameter).HasMaxLength(256);
        builder.Property(x => x.Evidence).HasMaxLength(4000);
        builder.Property(x => x.Remediation).HasMaxLength(4000);
        builder.Property(x => x.RemediationExampleConfig).HasMaxLength(4000);
        builder.Property(x => x.TurkishExecutiveSummary).HasMaxLength(2048);
        builder.Property(x => x.CheckCode).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Fingerprint).HasMaxLength(128);
        builder.Property(x => x.EligibilityReason).HasMaxLength(1024);
        builder.Property(x => x.ProgramPolicyMatch).HasMaxLength(64);
        builder.Property(x => x.ReflectionContentType).HasMaxLength(128);
        builder.Property(x => x.ReflectionLocation).HasMaxLength(256);
        builder.Property(x => x.InputSource).HasMaxLength(64);
        builder.Property(x => x.ReflectionMarker).HasMaxLength(128);

        builder.HasIndex(x => new { x.ScanResultId, x.Status });
        builder.HasIndex(x => new { x.CompanyId, x.Severity, x.Status });
        builder.HasIndex(x => x.Fingerprint);
        builder.HasIndex(x => x.CheckCode);
        builder.HasIndex(x => new { x.BugBountyEligible, x.SubmissionRecommendation });
        builder.HasIndex(x => x.FindingClass);
        builder.HasIndex(x => x.RootCauseGroupId);
        builder.HasIndex(x => x.BugBountySeverity);

        builder.HasOne(x => x.ScanResult)
            .WithMany(r => r.Findings)
            .HasForeignKey(x => x.ScanResultId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RootCauseGroup)
            .WithMany(g => g.Findings)
            .HasForeignKey(x => x.RootCauseGroupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class FindingStatusHistoryConfiguration : IEntityTypeConfiguration<FindingStatusHistory>
{
    public void Configure(EntityTypeBuilder<FindingStatusHistory> builder)
    {
        builder.ToTable("FindingStatusHistories");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Note).HasMaxLength(1024);

        builder.HasIndex(x => x.FindingId);

        builder.HasOne(x => x.Finding)
            .WithMany(f => f.StatusHistory)
            .HasForeignKey(x => x.FindingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RemediationRequestConfiguration : IEntityTypeConfiguration<RemediationRequest>
{
    public void Configure(EntityTypeBuilder<RemediationRequest> builder)
    {
        builder.ToTable("RemediationRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.InternalNotes).HasMaxLength(4000);
        builder.Property(x => x.ContactPreference).HasMaxLength(32);
        builder.Property(x => x.Currency).HasMaxLength(8);
        builder.Property(x => x.CompletionNote).HasMaxLength(2048);
        builder.Property(x => x.EstimatedPrice).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.CompanyId, x.Status });

        builder.HasOne(x => x.Finding)
            .WithMany(f => f.RemediationRequests)
            .HasForeignKey(x => x.FindingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Company)
            .WithMany(c => c.RemediationRequests)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RetestComparisonConfiguration : IEntityTypeConfiguration<RetestComparison>
{
    public void Configure(EntityTypeBuilder<RetestComparison> builder)
    {
        builder.ToTable("RetestComparisons");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ComparisonSummary).HasMaxLength(4000);

        builder.HasIndex(x => x.OriginalFindingId);
        builder.HasIndex(x => new { x.PreviousScanResultId, x.CurrentScanResultId });

        builder.HasOne(x => x.OriginalFinding)
            .WithMany(f => f.RetestComparisons)
            .HasForeignKey(x => x.OriginalFindingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PreviousScanResult)
            .WithMany()
            .HasForeignKey(x => x.PreviousScanResultId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CurrentScanResult)
            .WithMany()
            .HasForeignKey(x => x.CurrentScanResultId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
