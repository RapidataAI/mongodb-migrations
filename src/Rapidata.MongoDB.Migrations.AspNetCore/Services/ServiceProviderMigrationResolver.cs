using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Rapidata.MongoDB.Migrations.Contracts;
using Rapidata.MongoDB.Migrations.Services;

namespace Rapidata.MongoDB.Migrations.AspNetCore.Services;

public class ServiceProviderMigrationResolver : IMigrationResolver
{
    private readonly IServiceProvider _serviceProvider;

    public ServiceProviderMigrationResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IEnumerable<IMigration> GetMigrations(IEnumerable<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(
                type => typeof(IMigration).IsAssignableFrom(type)
                        && type is { IsInterface: false, IsAbstract: false })
            .Select(type => (IMigration)ActivatorUtilities.CreateInstance(_serviceProvider, type));
    }
}
