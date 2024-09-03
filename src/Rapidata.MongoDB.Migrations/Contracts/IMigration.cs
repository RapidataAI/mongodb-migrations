using MongoDB.Driver;

namespace Rapidata.MongoDB.Migrations.Contracts;

public interface IMigration : IBaseMigration
{
    string Name { get; }

    Task Migrate(IMongoDatabase database, CancellationToken cancellationToken);
}
