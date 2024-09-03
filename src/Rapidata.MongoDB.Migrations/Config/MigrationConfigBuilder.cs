using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Rapidata.MongoDB.Migrations.Config;

public sealed class MigrationConfigBuilder
{
    private readonly IDictionary<string, IList<Assembly>> _migrationAssembliesPerDatabase =
        new Dictionary<string, IList<Assembly>>(StringComparer.Ordinal);

    private string _collectionName = "_migrations";
    private string? _databaseName;
    private bool _failOnFailedMigrations = true;
    private IList<Assembly> _migrationAssemblies = new List<Assembly>();
    private TimeSpan? _migrationTimeout;
    private bool _rethrowExceptions;
    private bool _retryFailedMigrations;
    private bool _waitForRunningMigrations = true;
    private TimeSpan _waitForRunningMigrationsPollingInterval = TimeSpan.FromSeconds(1);

    private TimeSpan _waitForRunningMigrationsTimeout = TimeSpan.FromMinutes(5);

    public MigrationConfig Build(ILogger? logger = null)
    {
        if (_migrationAssembliesPerDatabase.Count != 0 &&
            (_databaseName is not null || _migrationAssemblies.Count != 0))
            logger?.LogWarning(
                "Can not use both WithMigrationsAssembliesPerDatabase and WithMigrationAssemblies or WithDatabaseName. Using WithMigrationsAssembliesPerDatabase");

        if (_migrationAssembliesPerDatabase.Count == 0 && _databaseName is null && _migrationAssemblies.Count == 0)
            logger?.LogWarning("No migration assemblies or database name provided. Will not run any migrations");

        if (_databaseName is not null && _migrationAssembliesPerDatabase.Count == 0)
            _migrationAssembliesPerDatabase.TryAdd(_databaseName, _migrationAssemblies);

        return new MigrationConfig
        {
            CollectionName = _collectionName,
            RetryFailedMigrations = _retryFailedMigrations,
            FailOnFailedMigrations = _failOnFailedMigrations,
            RethrowExceptions = _rethrowExceptions,
            WaitForRunningMigrations = _waitForRunningMigrations,
            MigrationTimeout = _migrationTimeout,
            MigrationAssembliesPerDatabase = _migrationAssembliesPerDatabase,
            WaitForRunningMigrationsTimeout = _waitForRunningMigrationsTimeout,
            WaitForRunningMigrationsPollingInterval = _waitForRunningMigrationsPollingInterval
        };
    }

    public MigrationConfigBuilder WithCollectionName(string collectionName)
    {
        _collectionName = collectionName;
        return this;
    }

    public MigrationConfigBuilder WithDatabaseName(string databaseName)
    {
        _databaseName = databaseName;
        return this;
    }

    public MigrationConfigBuilder ShouldRetryFailedMigrations(bool retryFailedMigrations)
    {
        _retryFailedMigrations = retryFailedMigrations;
        return this;
    }

    public MigrationConfigBuilder ShouldFailOnFailedMigrations(bool failOnFailedMigrations)
    {
        _failOnFailedMigrations = failOnFailedMigrations;
        return this;
    }

    public MigrationConfigBuilder ShouldRethrowExceptions(bool rethrowExceptions)
    {
        _rethrowExceptions = rethrowExceptions;
        return this;
    }

    public MigrationConfigBuilder ShouldWaitForRunningMigrations(bool waitForRunningMigrations)
    {
        _waitForRunningMigrations = waitForRunningMigrations;
        return this;
    }

    public MigrationConfigBuilder WithMigrationTimeout(TimeSpan? migrationTimeout)
    {
        _migrationTimeout = migrationTimeout;
        return this;
    }

    public MigrationConfigBuilder WithMigrationAssemblies(params Assembly[] migrationAssemblies)
    {
        _migrationAssemblies = migrationAssemblies;
        return this;
    }

    public MigrationConfigBuilder WithWaitForRunningMigrationsTimeout(TimeSpan waitForRunningMigrationsTimeout)
    {
        _waitForRunningMigrationsTimeout = waitForRunningMigrationsTimeout;
        return this;
    }

    public MigrationConfigBuilder WithWaitForRunningMigrationsPollingInterval(
        TimeSpan waitForRunningMigrationsPollingInterval)
    {
        _waitForRunningMigrationsPollingInterval = waitForRunningMigrationsPollingInterval;
        return this;
    }

    public MigrationConfigBuilder WithMigrationAssembliesForDatabase(string databaseName,
        params Assembly[] migrationAssemblies)
    {
        _migrationAssembliesPerDatabase.TryAdd(databaseName, migrationAssemblies);
        return this;
    }
}
