using System.Reflection;
using Microsoft.Extensions.Logging;
using Rapidata.MongoDB.Migrations.Config;
using Rapidata.MongoDB.Migrations.Contracts;
using Rapidata.MongoDB.Migrations.Entities;
using Rapidata.MongoDB.Migrations.Providers;
using Rapidata.MongoDB.Migrations.Repositories;
using Rapidata.MongoDB.Migrations.Utils;

namespace Rapidata.MongoDB.Migrations.Services;

public class MigrationService : IMigrationService
{
    private readonly MigrationConfig _config;
    private readonly ILogger<MigrationService> _logger;
    private readonly IMigrationResolver _migrationResolver;
    private readonly IMongoClientProvider _mongoClientProvider;

    public MigrationService(
        IMongoClientProvider mongoClientProvider,
        MigrationConfig config,
        IMigrationResolver migrationResolver,
        ILogger<MigrationService> logger)
    {
        _mongoClientProvider = mongoClientProvider;
        _config = config;
        _migrationResolver = migrationResolver;
        _logger = logger;
    }

    public async Task<IDictionary<string, IList<IMigration>>> GetMigrationsToExecutePerDatabase(
        CancellationToken cancellationToken)
    {
        var migrationsToExecutePerDatabase = new Dictionary<string, IList<IMigration>>();

        foreach (var (databaseName, assemblies) in _config.MigrationAssembliesPerDatabase)
        {
            migrationsToExecutePerDatabase[databaseName] =
                await GetMigrationsToExecute(databaseName, assemblies, cancellationToken)
                    .ConfigureAwait(false);
        }

        return migrationsToExecutePerDatabase;
    }

    public async Task ExecuteMigration(string databaseName, IMigration migration, CancellationToken cancellationToken)
    {
        if (!await CanExecuteMigrations(databaseName, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        await WaitForRunningMigrations(databaseName, cancellationToken).ConfigureAwait(false);

        var migrationRepository = GetMigrationRepository(databaseName);

        try
        {
            var hasMigrationLock = await migrationRepository
                .StartMigration(migration, cancellationToken)
                .ConfigureAwait(false);

            if (!hasMigrationLock)
            {
                _logger.LogInformation("Migration {MigrationName} is already running", migration.Name);
                return;
            }

            var client = _mongoClientProvider.GetClient();
            var database = client.GetDatabase(databaseName);
            
            // Merge cancellation token with timeout from config
            if (_config.MigrationTimeout is TimeSpan timeout)
            {
                var newCancellationTokenSource = new CancellationTokenSource(timeout);
                cancellationToken = CancellationTokenSource
                    .CreateLinkedTokenSource(cancellationToken, newCancellationTokenSource.Token)
                    .Token;
            }

            await migration.Migrate(database, cancellationToken)
                .ConfigureAwait(false);

            await migrationRepository
                .CompleteMigration(migration, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Migration {MigrationName} completed", migration.Name);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                var newCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                cancellationToken = newCancellationTokenSource.Token;
            }

            _logger.LogError(
                ex,
                "Migration {MigrationName} with version {MigrationVersion} failed",
                migration.Name,
                migration.Version);

            await migrationRepository
                .FailMigration(migration, cancellationToken)
                .ConfigureAwait(false);

            if (_config.RethrowExceptions)
            {
                throw;
            }
        }
    }

    public async Task<bool> CanExecuteMigrations(string databaseName, CancellationToken cancellationToken)
    {
        var migrationRepository = GetMigrationRepository(databaseName);

        if (!_config.FailOnFailedMigrations)
        {
            return true;
        }

        var hasFailedMigrations = await migrationRepository
            .HasFailedMigrations(cancellationToken)
            .ConfigureAwait(false);

        if (!hasFailedMigrations)
        {
            return true;
        }

        _logger.LogError("Failed migrations found for database {DatabaseName}", databaseName);
        return false;
    }

    public async Task WaitForRunningMigrations(string databaseName, CancellationToken cancellationToken)
    {
        var hasInformedAboutRunningMigrations = false;
        var migrationRepository = GetMigrationRepository(databaseName);

        var cancellationTokenSource = new CancellationTokenSource(_config.WaitForRunningMigrationsTimeout);
        var cancellationTokenWithTimeout = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token)
            .Token;

        var hasRunningMigrations = await migrationRepository
            .HasRunningMigrations(cancellationTokenWithTimeout)
            .ConfigureAwait(false);

        while (hasRunningMigrations)
        {
            if (!hasInformedAboutRunningMigrations)
            {
                _logger.LogInformation("Waiting for running migrations to complete");
                hasInformedAboutRunningMigrations = true;
            }
            
            await Task.Delay(_config.WaitForRunningMigrationsPollingInterval, cancellationTokenWithTimeout)
                .ConfigureAwait(false);

            hasRunningMigrations = await migrationRepository
                .HasRunningMigrations(cancellationTokenWithTimeout)
                .ConfigureAwait(false);
        }
    }

    private async Task<IList<IMigration>> GetMigrationsToExecute(
        string databaseName,
        IEnumerable<Assembly> assemblies,
        CancellationToken cancellationToken)
    {
        var migrationRepository = GetMigrationRepository(databaseName);

        var appliedMigrations = await migrationRepository
            .GetMigrations(cancellationToken)
            .ConfigureAwait(false);

        var migrationSet = new HashSet<IBaseMigration>(appliedMigrations, new MigrationEqualityComparer());

        return _migrationResolver
            .GetMigrations(assemblies)
            .Where(
                migration =>
                {
                    if (migrationSet.TryGetValue(migration, out var executedMigration))
                    {
                        return _config.RetryFailedMigrations && executedMigration is Migration
                        {
                            State: MigrationState.Failed,
                        };
                    }

                    return true;
                })
            .OrderBy(x => x.Date)
            .ThenBy(x => x.DeveloperId)
            .ThenBy(x => x.Version)
            .ToList();
    }

    private MigrationRepository GetMigrationRepository(string databaseName)
    {
        var client = _mongoClientProvider.GetClient();
        var database = client.GetDatabase(databaseName);

        return new MigrationRepository(database, _config);
    }
}
