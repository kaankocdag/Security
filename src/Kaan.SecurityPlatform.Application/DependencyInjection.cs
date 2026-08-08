using System.Reflection;
using FluentValidation;
using Mapster;
using Microsoft.Extensions.DependencyInjection;

namespace Kaan.SecurityPlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        var mapsterConfig = TypeAdapterConfig.GlobalSettings;
        mapsterConfig.Scan(assembly);
        services.AddSingleton(mapsterConfig);

        return services;
    }
}
