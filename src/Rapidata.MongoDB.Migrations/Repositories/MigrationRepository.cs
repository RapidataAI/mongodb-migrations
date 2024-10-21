using MongoDB.Driver;
using Rapidata.MongoDB.Migrations.Config;
using Rapidata.MongoDB.Migrations.Contracts;
using Rapidata.MongoDB.Migrations.Entities;

namespace Rapidata.MongoDB.Migrations.Repositories;

public class MigrationRepository : IMigrationRepository
{
    private readonly MigrationConfig _config;
    private readonly IMongoDatabase _database;

    public MigrationRepository(IMongoDatabase database, MigrationConfig config)
    {
        _database = database;
        _config = config;
    }

    public Task<bool> HasRunningMigrations(CancellationToken cancellationToken)
    {
        var collection = GetCollection();

        var filter = Builders<Migration>.Filter.Eq(x => x.State, MigrationState.Executing);

        return collection.Find(filter).AnyAsync(cancellationToken);
    }

    public Task<bool> HasFailedMigrations(CancellationToken cancellationToken)
    {
        var collection = GetCollection();

        var filter = Builders<Migration>.Filter.Eq(x => x.State, MigrationState.Failed);

        return collection.Find(filter).AnyAsync(cancellationToken);
    }

    public async Task<ICollection<Migration>> GetMigrations(CancellationToken cancellationToken)
    {
        var collection = GetCollection();

        var filter = Builders<Migration>.Filter.Empty;

        return await collection.Find(filter)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<short>> GetFailedMigrations(CancellationToken cancellationToken)
    {
        var collection = GetCollection();

        var filter = Builders<Migration>.Filter.Eq(x => x.State, MigrationState.Failed);

        return await collection
            .Find(filter)
            .Project(x => x.Version)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> StartMigration(IMigration migration, CancellationToken cancellationToken)
    {
        var collection = GetCollection();

        var filter = Builders<Migration>.Filter.Where(
            m => m.Date == migration.Date &&
                 m.DeveloperId == migration.DeveloperId &&
                 m.Version == migration.Version);

        var update = Builders<Migration>.Update
            .SetOnInsert(x => x.Name, migration.Name)
            .SetOnInsert(x => x.Date, migration.Date)
            .SetOnInsert(x => x.DeveloperId, migration.DeveloperId)
            .SetOnInsert(x => x.Version, migration.Version)
            .SetOnInsert(x => x.State, MigrationState.Executing)
            .SetOnInsert(x => x.AppliedAt, DateTime.UtcNow);

        var options = new UpdateOptions
        {
            IsUpsert = true,
        };

        var result = await collection
            .UpdateOneAsync(filter, update, options, cancellationToken)
            .ConfigureAwait(false);

        return result.UpsertedId != null;
    }

    public async Task CompleteMigration(IMigration migration, CancellationToken cancellationToken)
    {
        var collection = GetCollection();

        var filter = Builders<Migration>.Filter.Eq(x => x.Version, migration.Version) &
                     Builders<Migration>.Filter.Eq(x => x.DeveloperId, migration.DeveloperId);

        var update = Builders<Migration>.Update
            .Set(x => x.State, MigrationState.Applied);

        await collection
            .UpdateOneAsync(filter, update, new UpdateOptions(), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task FailMigration(IMigration migration, CancellationToken cancellationToken)
    {
        var collection = GetCollection();

        var filter = Builders<Migration>.Filter.Eq(x => x.Version, migration.Version) &
                     Builders<Migration>.Filter.Eq(x => x.DeveloperId, migration.DeveloperId);

        var update = Builders<Migration>.Update
            .Set(x => x.State, MigrationState.Failed);

        await collection
            .UpdateOneAsync(filter, update, new UpdateOptions(), cancellationToken)
            .ConfigureAwait(false);
    }

    private IMongoCollection<Migration> GetCollection()
    {
        return _database.GetCollection<Migration>(_config.CollectionName);
    }
}
