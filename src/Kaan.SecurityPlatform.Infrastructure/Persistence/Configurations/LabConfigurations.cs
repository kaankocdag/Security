using Kaan.SecurityPlatform.Domain.Entities.Lab;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Configurations;

public sealed class LabScenarioConfiguration : IEntityTypeConfiguration<LabScenario>
{
    public void Configure(EntityTypeBuilder<LabScenario> builder)
    {
        builder.ToTable("LabScenarios");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ScenarioKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TitleTr).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SummaryTr).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.VulnerableImageTag).HasMaxLength(256).IsRequired();
        builder.Property(x => x.PatchedImageTag).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.ScenarioKey).IsUnique();
    }
}

public sealed class LabTargetSiteConfiguration : IEntityTypeConfiguration<LabTargetSite>
{
    public void Configure(EntityTypeBuilder<LabTargetSite> builder)
    {
        builder.ToTable("LabTargetSites");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.HostName).HasMaxLength(253).IsRequired();
        builder.Property(x => x.NormalizedHostName).HasMaxLength(253).IsRequired();
        builder.Property(x => x.NotesTr).HasMaxLength(1000);
        builder.Property(x => x.CreatedByEmail).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.NormalizedHostName).IsUnique();
        builder.HasIndex(x => x.IsEnabled);
    }
}

public sealed class LabExecutionConfiguration : IEntityTypeConfiguration<LabExecution>
{
    public void Configure(EntityTypeBuilder<LabExecution> builder)
    {
        builder.ToTable("LabExecutions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ScenarioKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TargetHostName).HasMaxLength(253).IsRequired();
        builder.Property(x => x.ElevatedByEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.FailureReasonTr).HasMaxLength(2000);
        builder.Property(x => x.CancelReasonTr).HasMaxLength(1000);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.AuditCorrelationId);
        builder.HasIndex(x => x.LabTargetSiteId);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasOne(x => x.Environment)
            .WithOne(e => e.LabExecution!)
            .HasForeignKey<LabEnvironment>(e => e.LabExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Approval)
            .WithOne(a => a.LabExecution!)
            .HasForeignKey<LabAuthorizationApproval>(a => a.LabExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Comparison)
            .WithOne(c => c.LabExecution!)
            .HasForeignKey<LabComparisonResult>(c => c.LabExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Steps)
            .WithOne(s => s.LabExecution!)
            .HasForeignKey(s => s.LabExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Logs)
            .WithOne(l => l.LabExecution!)
            .HasForeignKey(l => l.LabExecutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LabEnvironmentConfiguration : IEntityTypeConfiguration<LabEnvironment>
{
    public void Configure(EntityTypeBuilder<LabEnvironment> builder)
    {
        builder.ToTable("LabEnvironments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.NetworkId).HasMaxLength(128);
        builder.Property(x => x.NetworkName).HasMaxLength(128);
        builder.Property(x => x.VulnerableContainerId).HasMaxLength(128);
        builder.Property(x => x.PatchedContainerId).HasMaxLength(128);
        builder.Property(x => x.InternalEndpoint).HasMaxLength(512);
        builder.HasIndex(x => x.LabExecutionId).IsUnique();
        builder.HasIndex(x => x.ExpiresAt);
    }
}

public sealed class LabExecutionStepConfiguration : IEntityTypeConfiguration<LabExecutionStep>
{
    public void Configure(EntityTypeBuilder<LabExecutionStep> builder)
    {
        builder.ToTable("LabExecutionSteps");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TitleTr).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SummaryTr).HasMaxLength(2000);
        builder.HasIndex(x => new { x.LabExecutionId, x.StepOrder }).IsUnique();
    }
}

public sealed class LabExecutionLogConfiguration : IEntityTypeConfiguration<LabExecutionLog>
{
    public void Configure(EntityTypeBuilder<LabExecutionLog> builder)
    {
        builder.ToTable("LabExecutionLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Level).HasMaxLength(32).IsRequired();
        builder.Property(x => x.MessageTr).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => new { x.LabExecutionId, x.LoggedAt });
    }
}

public sealed class LabComparisonResultConfiguration : IEntityTypeConfiguration<LabComparisonResult>
{
    public void Configure(EntityTypeBuilder<LabComparisonResult> builder)
    {
        builder.ToTable("LabComparisonResults");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RiskTr).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.WhyTr).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.FixTr).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.SummaryTr).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => x.LabExecutionId).IsUnique();
    }
}

public sealed class LabAuthorizationApprovalConfiguration : IEntityTypeConfiguration<LabAuthorizationApproval>
{
    public void Configure(EntityTypeBuilder<LabAuthorizationApproval> builder)
    {
        builder.ToTable("LabAuthorizationApprovals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ConfirmPhrase).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ClientIp).HasMaxLength(64).IsRequired();
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.HasIndex(x => x.LabExecutionId).IsUnique();
    }
}

public sealed class LabElevationTicketConfiguration : IEntityTypeConfiguration<LabElevationTicket>
{
    public void Configure(EntityTypeBuilder<LabElevationTicket> builder)
    {
        builder.ToTable("LabElevationTickets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ClientIp).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.TokenHash);
        builder.HasIndex(x => new { x.UserId, x.ExpiresAt });
    }
}
