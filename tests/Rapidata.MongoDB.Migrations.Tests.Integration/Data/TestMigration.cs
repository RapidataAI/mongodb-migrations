using MongoDB.Driver;
using Rapidata.MongoDB.Migrations.Contracts;

namespace Rapidata.MongoDB.Migrations.Tests.Integration.Data;

public class TestMigration : IMigration
{
    public DateOnly Date => new(2024, 10, 10);
    public short Version => 1;
    public string Name => "Test Migration";

    public async Task Migrate(IMongoDatabase database, CancellationToken cancellationToken)
    {
        await database.CreateCollectionAsync(nameof(TestMigration), null, cancellationToken);
    }
}