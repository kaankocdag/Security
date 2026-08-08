using Kaan.SecurityPlatform.Domain.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Configurations;

public sealed class SecurityProjectConfiguration : IEntityTypeConfiguration<SecurityProject>
{
    public void Configure(EntityTypeBuilder<SecurityProject> builder)
    {
        builder.ToTable("SecurityProjects");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2048);
        builder.Property(x => x.PrimaryContactEmail).HasMaxLength(320);

        builder.HasIndex(x => new { x.CompanyId, x.Name });
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Company)
            .WithMany(c => c.Projects)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DomainAssetConfiguration : IEntityTypeConfiguration<DomainAsset>
{
    public void Configure(EntityTypeBuilder<DomainAsset> builder)
    {
        builder.ToTable("DomainAssets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.HostName).HasMaxLength(253).IsRequired();
        builder.Property(x => x.NormalizedHostName).HasMaxLength(253).IsRequired();
        builder.Property(x => x.Scheme).HasMaxLength(16).IsRequired();
        builder.Property(x => x.VerificationToken).HasMaxLength(128);
        builder.Property(x => x.LastVerificationError).HasMaxLength(1024);
        builder.Property(x => x.Notes).HasMaxLength(1024);
        builder.Property(x => x.Source).HasMaxLength(32).IsRequired().HasDefaultValue("Manual");
        builder.Property(x => x.HackerOneProgramHandle).HasMaxLength(128);
        builder.Property(x => x.HackerOneProgramName).HasMaxLength(256);
        builder.Property(x => x.HackerOneScopeId).HasMaxLength(64);
        builder.Property(x => x.HackerOneAssetType).HasMaxLength(64);
        builder.Property(x => x.HackerOneMaxSeverity).HasMaxLength(32);
        builder.Property(x => x.HackerOneCurrency).HasMaxLength(16);
        builder.Property(x => x.HackerOneSubmissionState).HasMaxLength(64);
        builder.Property(x => x.HackerOneBountySummary).HasMaxLength(512);

        // Same host may appear under multiple H1 programs; Manual rows use null handle.
        builder.HasIndex(x => new { x.SecurityProjectId, x.NormalizedHostName, x.HackerOneProgramHandle })
            .IsUnique()
            .HasFilter("[HackerOneProgramHandle] IS NOT NULL");
        builder.HasIndex(x => new { x.SecurityProjectId, x.NormalizedHostName })
            .IsUnique()
            .HasFilter("[HackerOneProgramHandle] IS NULL")
            .HasDatabaseName("IX_DomainAssets_SecurityProjectId_NormalizedHostName_Manual");
        builder.HasIndex(x => x.NormalizedHostName);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.IsVerified);
        builder.HasIndex(x => x.Source);
        builder.HasIndex(x => x.HackerOneProgramHandle);
        builder.HasIndex(x => x.HackerOneEligibleForBounty);

        builder.HasOne(x => x.SecurityProject)
            .WithMany(p => p.Domains)
            .HasForeignKey(x => x.SecurityProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AuthorizationRecordConfiguration : IEntityTypeConfiguration<AuthorizationRecord>
{
    public void Configure(EntityTypeBuilder<AuthorizationRecord> builder)
    {
        builder.ToTable("AuthorizationRecords");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AuthorizedByName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.AuthorizedByEmail).HasMaxLength(320).IsRequired();
        builder.Property(x => x.AuthorizedByTitle).HasMaxLength(128);
        builder.Property(x => x.AuthorizationScope).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.AllowedTestTypes).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.ForbiddenActions).HasMaxLength(2048);
        builder.Property(x => x.ApprovalEvidencePath).HasMaxLength(512);
        builder.Property(x => x.RevocationReason).HasMaxLength(512);

        builder.HasIndex(x => new { x.SecurityProjectId, x.DomainAssetId, x.Status });

        builder.HasOne(x => x.SecurityProject)
            .WithMany(p => p.Authorizations)
            .HasForeignKey(x => x.SecurityProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DomainAsset)
            .WithMany(d => d.Authorizations)
            .HasForeignKey(x => x.DomainAssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
