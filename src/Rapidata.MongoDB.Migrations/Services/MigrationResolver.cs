using System.Reflection;
using Rapidata.MongoDB.Migrations.Contracts;
using Rapidata.MongoDB.Migrations.Entities;

namespace Rapidata.MongoDB.Migrations.Services;

public class MigrationResolver : IMigrationResolver
{
    public IEnumerable<IMigration> GetMigrations(
        IEnumerable<Assembly> assemblies,
        HashSet<IBaseMigration> executedMigrations,
        bool retryFailedMigrations)
    {
        return GetMigrations(assemblies)
            .Where(migration =>
            {
                if (executedMigrations.TryGetValue(migration, out var executedMigration))
                    return retryFailedMigrations && executedMigration is Migration { State: MigrationState.Failed };

                return true;
            });
    }

    private static IEnumerable<IMigration> GetMigrations(IEnumerable<Assembly> assemblies)
    {
        return assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IMigration).IsAssignableFrom(type)
                           && type is { IsInterface: false, IsAbstract: false })
            .Select(Activator.CreateInstance)
            .OfType<IMigration>();
    }
}
