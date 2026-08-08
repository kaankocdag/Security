using Kaan.SecurityPlatform.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kaan.SecurityPlatform.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.JwtId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RevokedByIp).HasMaxLength(64);
        builder.Property(x => x.RevocationReason).HasMaxLength(256);
        builder.Property(x => x.CreatedByIp).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);

        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ExpiresAt);

        builder.HasOne(x => x.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.FirstName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RejectionReason).HasMaxLength(512);
        builder.Property(x => x.SuspensionReason).HasMaxLength(512);
        builder.Property(x => x.PreferredLanguage).HasMaxLength(16);
        builder.Property(x => x.AvatarPath).HasMaxLength(512);
        builder.Property(x => x.JobTitle).HasMaxLength(128);
        builder.Property(x => x.PhoneCountryCode).HasMaxLength(8);

        builder.Ignore(x => x.FullName);

        builder.HasIndex(x => x.MembershipStatus);
        builder.HasIndex(x => x.PrimaryCompanyId);
    }
}
