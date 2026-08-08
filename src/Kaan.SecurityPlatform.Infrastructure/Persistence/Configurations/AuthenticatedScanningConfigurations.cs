using Kaan.SecurityPlatform.Domain.Entities.AuthenticatedScanning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Configurations;

public sealed class TargetTestAccountConfiguration : IEntityTypeConfiguration<TargetTestAccount>
{
    public void Configure(EntityTypeBuilder<TargetTestAccount> builder)
    {
        builder.ToTable("TargetTestAccounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TargetDomain).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Username).HasMaxLength(128);
        builder.Property(x => x.DisplayName).HasMaxLength(128);
        builder.Property(x => x.EncryptedSecretReference).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.RegistrationUrl).HasMaxLength(1024);
        builder.Property(x => x.LoginUrl).HasMaxLength(1024);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.HasIndex(x => new { x.TargetId, x.Role });
        builder.HasOne(x => x.Target).WithMany().HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.Restrict);
        // NoAction avoids DomainAsset → Profile/Account multiple cascade paths on SQL Server.
        builder.HasOne(x => x.IdentityProfile).WithMany().HasForeignKey(x => x.IdentityProfileId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class TestIdentityProfileConfiguration : IEntityTypeConfiguration<TestIdentityProfile>
{
    public void Configure(EntityTypeBuilder<TestIdentityProfile> builder)
    {
        builder.ToTable("TestIdentityProfiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProfileName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.TargetDomain).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(128);
        builder.Property(x => x.FirstName).HasMaxLength(128);
        builder.Property(x => x.LastName).HasMaxLength(128);
        builder.Property(x => x.DisplayName).HasMaxLength(128);
        builder.Property(x => x.Country).HasMaxLength(128);
        builder.Property(x => x.ProgramName).HasMaxLength(256);
        builder.Property(x => x.ProgramUrl).HasMaxLength(512);
        builder.Property(x => x.AccountPurpose).HasMaxLength(256);
        builder.HasIndex(x => x.TargetId);
        builder.HasOne(x => x.Target).WithMany().HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AuthenticatedScanRunConfiguration : IEntityTypeConfiguration<AuthenticatedScanRun>
{
    public void Configure(EntityTypeBuilder<AuthenticatedScanRun> builder)
    {
        builder.ToTable("AuthenticatedScanRuns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TakeoverMessage).HasMaxLength(1024);
        builder.Property(x => x.StopReason).HasMaxLength(512);
        builder.Property(x => x.ErrorCode).HasMaxLength(64);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.LoginUrlUsed).HasMaxLength(1024);
        builder.HasIndex(x => x.TargetId);
        builder.HasOne(x => x.Target).WithMany().HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TestAccount).WithMany().HasForeignKey(x => x.TestAccountId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ScanModeObservationConfiguration : IEntityTypeConfiguration<ScanModeObservation>
{
    public void Configure(EntityTypeBuilder<ScanModeObservation> builder)
    {
        builder.ToTable("ScanModeObservations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.FinalUrl).HasMaxLength(1024);
        builder.Property(x => x.RedirectChain).HasMaxLength(2000);
        builder.Property(x => x.ContentType).HasMaxLength(256);
        builder.Property(x => x.ResponseHash).HasMaxLength(64);
        builder.Property(x => x.RedactedEvidence).HasMaxLength(2000);
        builder.Property(x => x.MaskedAccountLabel).HasMaxLength(128);
        builder.HasIndex(x => x.AuthenticatedScanRunId);
        builder.HasOne(x => x.AuthenticatedScanRun).WithMany(r => r.Observations)
            .HasForeignKey(x => x.AuthenticatedScanRunId).OnDelete(DeleteBehavior.Cascade);
    }
}
