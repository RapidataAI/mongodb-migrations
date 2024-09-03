using Rapidata.MongoDB.Migrations.Contracts;
using Rapidata.MongoDB.Migrations.Entities;

namespace Rapidata.MongoDB.Migrations.Repositories;

public interface IMigrationRepository
{
    Task<bool> HasRunningMigrations(CancellationToken cancellationToken);

    Task<bool> HasFailedMigrations(CancellationToken cancellationToken);

    Task<ICollection<Migration>> GetMigrations(CancellationToken cancellationToken);

    Task<IEnumerable<short>> GetFailedMigrations(CancellationToken cancellationToken);

    Task<bool> StartMigration(IMigration migration, CancellationToken cancellationToken);

    Task CompleteMigration(IMigration migration, CancellationToken cancellationToken);

    Task FailMigration(IMigration migration, CancellationToken cancellationToken);
}