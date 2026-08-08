using Kaan.SecurityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Kaan.SecurityPlatform.IntegrationTests;

public sealed class KaanApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            RemoveDbContextRegistrations<SecurityPlatformDbContext>(services);

            services.AddDbContext<SecurityPlatformDbContext>(options =>
            {
                options.UseInMemoryDatabase($"kaan-tests-{Guid.NewGuid():N}");
            });
        });
    }

    private static void RemoveDbContextRegistrations<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        var toRemove = services
            .Where(d =>
                d.ServiceType == typeof(TContext) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(DbContextOptions<TContext>) ||
                d.ServiceType == typeof(IDbContextOptionsConfiguration<TContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>) &&
                 d.ServiceType.GenericTypeArguments[0] == typeof(TContext)))
            .ToList();

        foreach (var descriptor in toRemove)
        {
            services.Remove(descriptor);
        }
    }
}
