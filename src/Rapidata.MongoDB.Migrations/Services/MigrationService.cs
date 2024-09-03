using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Rapidata.MongoDB.Migrations.Config;
using Rapidata.MongoDB.Migrations.Contracts;
using Rapidata.MongoDB.Migrations.Repositories;
using Rapidata.MongoDB.Migrations.Utils;

namespace Rapidata.MongoDB.Migrations.Services;

public class MigrationService : IMigrationService
{
    private readonly MigrationConfig _config;
    private readonly ILogger<MigrationService> _logger;
    private readonly ImmutableDictionary<string, IMigrationRepository> _migrationRepositories;
    private readonly IMigrationResolver _migrationResolver;
    private readonly IMongoClient _mongoClient;

    public MigrationService(
        IMongoClient mongoClient,
        MigrationConfig config,
        IMigrationResolver migrationResolver,
        ILogger<MigrationService> logger)
    {
        _mongoClient = mongoClient;
        _config = config;
        _migrationResolver = migrationResolver;
        _logger = logger;

        _migrationRepositories = config.MigrationAssembliesPerDatabase.Keys
            .Select(databaseName => new KeyValuePair<string, IMigrationRepository>(
                databaseName,
                new MigrationRepository(mongoClient.GetDatabase(databaseName), config)))
            .ToImmutableDictionary(pair => pair.Key, pair => pair.Value);
    }

    public async Task<IDictionary<string, IList<IMigration>>> GetMigrationsToExecutePerDatabase(
        CancellationToken cancellationToken)
    {
        var migrationsToExecutePerDatabase = new Dictionary<string, IList<IMigration>>();

        foreach (var (databaseName, assemblies) in _config.MigrationAssembliesPerDatabase)
            migrationsToExecutePerDatabase[databaseName] =
                await GetMigrationsToExecute(databaseName, assemblies, cancellationToken)
                    .ConfigureAwait(false);

        return migrationsToExecutePerDatabase;
    }

    public async Task ExecuteMigration(string databaseName, IMigration migration, CancellationToken cancellationToken)
    {
        if (!await CanExecuteMigrations(databaseName, cancellationToken).ConfigureAwait(false)) return;

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

            _logger.LogInformation("Executing migration {MigrationName} with version {MigrationVersion}",
                migration.Name, migration.Version);

            var database = _mongoClient.GetDatabase(databaseName);

            await migration.Migrate(database, cancellationToken)
                .ConfigureAwait(false);

            await migrationRepository
                .CompleteMigration(migration, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogInformation("Migration {MigrationName} with version {MigrationVersion} completed",
                migration.Name, migration.Version);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                var newCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                cancellationToken = newCancellationTokenSource.Token;
            }

            _logger.LogError(ex, "Migration {MigrationName} with version {MigrationVersion} failed",
                migration.Name, migration.Version);

            await migrationRepository
                .FailMigration(migration, cancellationToken)
                .ConfigureAwait(false);

            if (_config.RethrowExceptions) throw;
        }
    }

    public async Task<bool> CanExecuteMigrations(string databaseName, CancellationToken cancellationToken)
    {
        var migrationRepository = GetMigrationRepository(databaseName);

        if (_config.FailOnFailedMigrations)
        {
            var hasFailedMigrations = await migrationRepository
                .HasFailedMigrations(cancellationToken)
                .ConfigureAwait(false);

            if (hasFailedMigrations)
            {
                _logger.LogError("Failed migrations found for database {DatabaseName}", databaseName);
                return false;
            }
        }

        return true;
    }

    public async Task WaitForRunningMigrations(string databaseName, CancellationToken cancellationToken)
    {
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
            .GetMigrations(assemblies, migrationSet, _config.RetryFailedMigrations)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.DeveloperId)
            .ThenBy(x => x.Version)
            .ToList();
    }

    private IMigrationRepository GetMigrationRepository(string databaseName)
    {
        if (!_migrationRepositories.TryGetValue(databaseName, out var repository))
            throw new InvalidOperationException($"No migration repository found for database {databaseName}");

        return repository;
    }
}
