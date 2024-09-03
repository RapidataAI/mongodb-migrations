using MongoDB.Driver;
using Rapidata.MongoDB.Migrations.Configuration;
using Testcontainers.MongoDb;

namespace Rapidata.MongoDB.Migrations.Tests.Integration;

[SetUpFixture]
public class MongoFixture
{
    private static MongoDbContainer _container = null!;
    private static MongoClient _client = null!;

    public static MongoClient Client => _client;

    [OneTimeSetUp]
    public static async Task Setup()
    {
        _container = new MongoDbBuilder().WithPortBinding(7521, MongoDbBuilder.MongoDbPort).Build();

        await _container.StartAsync().ConfigureAwait(false);

        _client = new MongoClient(_container.GetConnectionString());
        EntityConfiguration.ConfigureMigration();
    }

    [OneTimeTearDown]
    public static async Task TearDown()
    {
        await _container.DisposeAsync().ConfigureAwait(false);
    }
}
