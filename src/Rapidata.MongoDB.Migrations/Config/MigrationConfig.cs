using System.Reflection;

namespace Rapidata.MongoDB.Migrations.Config;

public sealed record MigrationConfig
{
    public required string CollectionName { get; init; }

    public required bool RetryFailedMigrations { get; init; }

    public required bool FailOnFailedMigrations { get; init; }

    public required bool RethrowExceptions { get; init; }

    public required bool WaitForRunningMigrations { get; init; }

    public required TimeSpan? MigrationTimeout { get; init; }

    public required IDictionary<string, IList<Assembly>> MigrationAssembliesPerDatabase { get; init; }

    public required TimeSpan WaitForRunningMigrationsTimeout { get; init; }

    public required TimeSpan WaitForRunningMigrationsPollingInterval { get; init; }
}
