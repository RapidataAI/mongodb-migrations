using System.Reflection;
using Rapidata.MongoDB.Migrations.Contracts;

namespace Rapidata.MongoDB.Migrations.Services;

public class MigrationResolver : IMigrationResolver
{
    public IEnumerable<IMigration> GetMigrations(IEnumerable<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(
                type => typeof(IMigration).IsAssignableFrom(type)
                        && type is { IsInterface: false, IsAbstract: false })
            .Select(Activator.CreateInstance)
            .OfType<IMigration>();
    }
}
