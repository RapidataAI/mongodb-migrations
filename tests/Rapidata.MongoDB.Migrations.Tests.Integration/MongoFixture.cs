using MongoDB.Driver;
using Rapidata.MongoDB.Migrations.Configuration;
using Testcontainers.MongoDb;

namespace Rapidata.MongoDB.Migrations.Tests.Integration;

[SetUpFixture]
public class MongoFixture
{
    private static MongoDbContainer _container = null!;

    public static MongoClient Client { get; private set; } = null!;

    [OneTimeSetUp]
    public async static Task Setup()
    {
        _container = new MongoDbBuilder().WithPortBinding(hostPort: 7521, MongoDbBuilder.MongoDbPort).Build();

        await _container.StartAsync().ConfigureAwait(false);

        Client = new MongoClient(_container.GetConnectionString());
        EntityConfiguration.ConfigureMigration();
    }

    [OneTimeTearDown]
    public async static Task TearDown()
    {
        Client.Dispose();
        await _container.DisposeAsync().ConfigureAwait(false);
    }
}
