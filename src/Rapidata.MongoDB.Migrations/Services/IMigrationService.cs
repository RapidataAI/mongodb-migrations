using Rapidata.MongoDB.Migrations.Contracts;

namespace Rapidata.MongoDB.Migrations.Services;

public interface IMigrationService
{
    Task<IDictionary<string, IList<IMigration>>> GetMigrationsToExecutePerDatabase(CancellationToken cancellationToken);

    Task ExecuteMigration(string databaseName, IMigration migration, CancellationToken cancellationToken);

    Task<bool> CanExecuteMigrations(string databaseName, CancellationToken cancellationToken);
}
