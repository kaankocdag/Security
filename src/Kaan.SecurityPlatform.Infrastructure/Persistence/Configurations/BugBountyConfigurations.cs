using Kaan.SecurityPlatform.Domain.Entities.BugBounty;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Configurations;

public sealed class BugBountyProgramConfiguration : IEntityTypeConfiguration<BugBountyProgram>
{
    public void Configure(EntityTypeBuilder<BugBountyProgram> builder)
    {
        builder.ToTable("BugBountyPrograms");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PolicyKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Handle).HasMaxLength(128).IsRequired();
        builder.Property(x => x.OpenReportUrl).HasMaxLength(512);
        builder.Property(x => x.ExternalProgramId).HasMaxLength(128);
        builder.Property(x => x.Currency).HasMaxLength(16);
        builder.Property(x => x.SubmissionState).HasMaxLength(64);
        builder.Property(x => x.State).HasMaxLength(64);
        builder.HasIndex(x => x.PolicyKey).IsUnique();
        builder.HasIndex(x => x.Handle).IsUnique();
    }
}

public sealed class BugBountyPolicyRuleConfiguration : IEntityTypeConfiguration<BugBountyPolicyRule>
{
    public void Configure(EntityTypeBuilder<BugBountyPolicyRule> builder)
    {
        builder.ToTable("BugBountyPolicyRules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Notes).HasMaxLength(1024);
        builder.HasIndex(x => new { x.BugBountyProgramId, x.PolicyCategory }).IsUnique();
        builder.HasOne(x => x.Program)
            .WithMany(p => p.PolicyRules)
            .HasForeignKey(x => x.BugBountyProgramId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RootCauseGroupConfiguration : IEntityTypeConfiguration<RootCauseGroup>
{
    public void Configure(EntityTypeBuilder<RootCauseGroup> builder)
    {
        builder.ToTable("RootCauseGroups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FingerprintKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(2048);
        builder.HasIndex(x => x.FingerprintKey).IsUnique();
    }
}

public sealed class HackerOneReportDraftConfiguration : IEntityTypeConfiguration<HackerOneReportDraft>
{
    public void Configure(EntityTypeBuilder<HackerOneReportDraft> builder)
    {
        builder.ToTable("HackerOneReportDrafts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Asset).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Weakness).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Impact).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.StepsToReproduce).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.ProofOfConcept).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.MarkdownBody).HasMaxLength(16000);
        builder.Property(x => x.TurkishMarkdownBody).HasMaxLength(16000);
        builder.HasIndex(x => x.FindingId);
        builder.HasIndex(x => x.Status);
        builder.HasOne(x => x.Finding)
            .WithMany()
            .HasForeignKey(x => x.FindingId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Program)
            .WithMany(p => p.ReportDrafts)
            .HasForeignKey(x => x.BugBountyProgramId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HackerOneSubmissionRecordConfiguration : IEntityTypeConfiguration<HackerOneSubmissionRecord>
{
    public void Configure(EntityTypeBuilder<HackerOneSubmissionRecord> builder)
    {
        builder.ToTable("HackerOneSubmissionRecords");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalReportId).HasMaxLength(128);
        builder.Property(x => x.ExternalReportUrl).HasMaxLength(512);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2048);
        builder.HasIndex(x => x.HackerOneReportDraftId);
        builder.HasOne(x => x.Draft)
            .WithMany(d => d.Submissions)
            .HasForeignKey(x => x.HackerOneReportDraftId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class BugBountyAuditLogConfiguration : IEntityTypeConfiguration<BugBountyAuditLog>
{
    public void Configure(EntityTypeBuilder<BugBountyAuditLog> builder)
    {
        builder.ToTable("BugBountyAuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ActorEmail).HasMaxLength(256);
        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(64);
        builder.Property(x => x.DetailsJson).HasMaxLength(8000);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.Action);
    }
}

public sealed class ScanProfileConfiguration : IEntityTypeConfiguration<ScanProfile>
{
    public void Configure(EntityTypeBuilder<ScanProfile> builder)
    {
        builder.ToTable("ScanProfiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProfileKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.UserAgentConfigKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RateLimitPerMinuteConfigKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1024);
        builder.HasIndex(x => x.ProfileKey).IsUnique();
    }
}

public sealed class HackerOneWorkspaceSettingsConfiguration : IEntityTypeConfiguration<HackerOneWorkspaceSettings>
{
    public void Configure(EntityTypeBuilder<HackerOneWorkspaceSettings> builder)
    {
        builder.ToTable("HackerOneWorkspaceSettings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OpenReportUrlTemplate).HasMaxLength(512).IsRequired();
        builder.HasOne(x => x.DefaultProgram)
            .WithMany()
            .HasForeignKey(x => x.DefaultBugBountyProgramId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class HackerOneApiCredentialConfiguration : IEntityTypeConfiguration<HackerOneApiCredential>
{
    public void Configure(EntityTypeBuilder<HackerOneApiCredential> builder)
    {
        builder.ToTable("HackerOneApiCredentials");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Identifier).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ProtectedApiToken).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ApiUsername).HasMaxLength(256);
        builder.HasIndex(x => x.Identifier).IsUnique();
    }
}
