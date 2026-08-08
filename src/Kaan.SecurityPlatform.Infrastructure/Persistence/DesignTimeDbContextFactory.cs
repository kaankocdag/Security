using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kaan.SecurityPlatform.Infrastructure.Persistence;

/// <summary>
/// dotnet ef migrations komutları için tasarım zamanı DbContext üreticisi.
/// Runtime ile aynı LocalDB veritabanını kullanır.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SecurityPlatformDbContext>
{
    public const string DefaultConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=KaanSecurityPlatform;Trusted_Connection=True;Encrypt=False;MultipleActiveResultSets=true";

    public SecurityPlatformDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("KAAN_DESIGN_CONNSTR")
            ?? DefaultConnectionString;

        var optionsBuilder = new DbContextOptionsBuilder<SecurityPlatformDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsAssembly(typeof(SecurityPlatformDbContext).Assembly.FullName);
        });

        return new SecurityPlatformDbContext(optionsBuilder.Options);
    }
}
