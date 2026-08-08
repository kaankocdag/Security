using Kaan.SecurityPlatform.Domain.Entities.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Configurations;

public sealed class FindingValidationRunConfiguration : IEntityTypeConfiguration<FindingValidationRun>
{
    public void Configure(EntityTypeBuilder<FindingValidationRun> builder)
    {
        builder.ToTable("FindingValidationRuns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ValidatorType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.StopReason).HasMaxLength(512);
        builder.Property(x => x.ErrorCode).HasMaxLength(64);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(x => x.FindingId);
        builder.HasIndex(x => new { x.TargetId, x.Status });
        builder.HasOne(x => x.Finding).WithMany().HasForeignKey(x => x.FindingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Target).WithMany().HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.Restrict);
        // NoAction: SQL Server rejects multiple cascade paths via DomainAsset → Scope/AuthEvidence → Run.
        builder.HasOne(x => x.AuthorizationEvidence).WithMany().HasForeignKey(x => x.AuthorizationEvidenceId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.ScopePolicy).WithMany().HasForeignKey(x => x.ScopePolicyId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.Result).WithOne(r => r.ValidationRun)
            .HasForeignKey<FindingValidationResult>(r => r.ValidationRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class FindingValidationResultConfiguration : IEntityTypeConfiguration<FindingValidationResult>
{
    public void Configure(EntityTypeBuilder<FindingValidationResult> builder)
    {
        builder.ToTable("FindingValidationResults");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EligibilityReason).HasMaxLength(2000);
        builder.Property(x => x.ManualReviewReasons).HasMaxLength(4000);
        builder.Property(x => x.ExpectedResult).HasMaxLength(4000);
        builder.Property(x => x.ActualResult).HasMaxLength(4000);
        builder.Property(x => x.ValidatorVersion).HasMaxLength(32);
        builder.Property(x => x.TestAccountRolesUsed).HasMaxLength(256);
        builder.HasIndex(x => x.ValidationRunId).IsUnique();
    }
}

public sealed class ValidationEvidenceConfiguration : IEntityTypeConfiguration<ValidationEvidence>
{
    public void Configure(EntityTypeBuilder<ValidationEvidence> builder)
    {
        builder.ToTable("ValidationEvidence");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RequestMethod).HasMaxLength(16).IsRequired();
        builder.Property(x => x.RedactedRequestUrl).HasMaxLength(1024);
        builder.Property(x => x.FinalUrl).HasMaxLength(1024);
        builder.Property(x => x.RedirectChain).HasMaxLength(2000);
        builder.Property(x => x.ResponseContentType).HasMaxLength(256);
        builder.Property(x => x.ResponseHash).HasMaxLength(64);
        builder.Property(x => x.RedactedResponseExcerpt).HasMaxLength(2000);
        builder.HasIndex(x => x.ValidationRunId);
        builder.HasOne(x => x.ValidationRun).WithMany(r => r.EvidenceItems)
            .HasForeignKey(x => x.ValidationRunId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ScopePolicyConfiguration : IEntityTypeConfiguration<ScopePolicy>
{
    public void Configure(EntityTypeBuilder<ScopePolicy> builder)
    {
        builder.ToTable("ScopePolicies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProgramName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ProgramUrl).HasMaxLength(512);
        builder.Property(x => x.AllowedTestMethods).HasMaxLength(512);
        builder.Property(x => x.ProhibitedTestMethods).HasMaxLength(1024);
        builder.Property(x => x.PolicyEvidence).HasMaxLength(4000);
        builder.HasIndex(x => x.TargetId);
        builder.HasOne(x => x.Target).WithMany().HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ValidationAuthorizationEvidenceConfiguration : IEntityTypeConfiguration<ValidationAuthorizationEvidence>
{
    public void Configure(EntityTypeBuilder<ValidationAuthorizationEvidence> builder)
    {
        builder.ToTable("ValidationAuthorizationEvidence");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AuthorizedByName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.AuthorizedByEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ScopeSummary).HasMaxLength(2000);
        builder.Property(x => x.AllowedTestTypes).HasMaxLength(512);
        builder.Property(x => x.EvidenceNotes).HasMaxLength(2000);
        builder.HasIndex(x => x.TargetId);
        builder.HasOne(x => x.Target).WithMany().HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.AuthorizationRecord).WithMany().HasForeignKey(x => x.AuthorizationRecordId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class TestAccountSessionConfiguration : IEntityTypeConfiguration<TestAccountSession>
{
    public void Configure(EntityTypeBuilder<TestAccountSession> builder)
    {
        builder.ToTable("TestAccountSessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).HasMaxLength(128).IsRequired();
        builder.Property(x => x.EncryptedSecretReference).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.OwnedTestResourceHint).HasMaxLength(1024);
        builder.HasIndex(x => new { x.TargetId, x.Role });
        builder.HasOne(x => x.Target).WithMany().HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.Cascade);
    }
}
