using Kaan.SecurityPlatform.Domain.Entities.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.LegalName).HasMaxLength(256);
        builder.Property(x => x.TaxNumber).HasMaxLength(32);
        builder.Property(x => x.ContactName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ContactEmail).HasMaxLength(320).IsRequired();
        builder.Property(x => x.ContactPhone).HasMaxLength(40);
        builder.Property(x => x.WebsiteUrl).HasMaxLength(512);
        builder.Property(x => x.Industry).HasMaxLength(128);
        builder.Property(x => x.Country).HasMaxLength(80);
        builder.Property(x => x.City).HasMaxLength(80);
        builder.Property(x => x.SuspensionReason).HasMaxLength(512);
        builder.Property(x => x.Notes).HasMaxLength(2048);

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ContactEmail).IsUnique();
    }
}

public sealed class CompanyUserConfiguration : IEntityTypeConfiguration<CompanyUser>
{
    public void Configure(EntityTypeBuilder<CompanyUser> builder)
    {
        builder.ToTable("CompanyUsers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvitationEmail).HasMaxLength(320);

        builder.HasIndex(x => new { x.CompanyId, x.UserId }).IsUnique();
        builder.HasIndex(x => x.UserId);

        builder.HasOne(x => x.Company)
            .WithMany(c => c.Members)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
