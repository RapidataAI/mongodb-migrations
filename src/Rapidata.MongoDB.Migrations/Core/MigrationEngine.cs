using Microsoft.Extensions.Logging;
using Rapidata.MongoDB.Migrations.Configuration;
using Rapidata.MongoDB.Migrations.Contracts;
using Rapidata.MongoDB.Migrations.Services;

namespace Rapidata.MongoDB.Migrations.Core;

public sealed class MigrationEngine
{
    private readonly ILogger<MigrationEngine> _logger;
    private readonly IMigrationService _migrationService;

    public MigrationEngine(IMigrationService migrationService, ILogger<MigrationEngine> logger)
    {
        _migrationService = migrationService;
        _logger = logger;
    }

    public async Task Migrate(CancellationToken cancellationToken)
    {
        EntityConfiguration.ConfigureMigration();

        var migrationsPerDatabase = await _migrationService.GetMigrationsToExecutePerDatabase(cancellationToken)
            .ConfigureAwait(false);

        var tasks = migrationsPerDatabase.Select(pair =>
            ExecuteMigrationsForDatabase(pair.Key, pair.Value, cancellationToken));

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ExecuteMigrationsForDatabase(
        string databaseName,
        IList<IMigration> migrations,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executing migrations for database {DatabaseName}", databaseName);

        await _migrationService.CanExecuteMigrations(databaseName, cancellationToken)
            .ConfigureAwait(false);

        for (var i = 0; i < migrations.Count; i++)
        {
            var migration = migrations[i];
            _logger.LogInformation(
                "Executing migration {MigrationName} with version {MigrationVersion} for database {DatabaseName}. ({MigrationIndex}/{MigrationCount})",
                migration.Name, migration.Version, databaseName, i + 1, migrations.Count);

            await _migrationService.ExecuteMigration(databaseName, migration, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
