using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Rapidata.MongoDB.Migrations.AspNetCore.Startup;
using Rapidata.MongoDB.Migrations.Core;
using Rapidata.MongoDB.Migrations.Entities;
using Rapidata.MongoDB.Migrations.Tests.Integration.Data;

namespace Rapidata.MongoDB.Migrations.Tests.Integration.Injection;

public class DiTests
{
    [Test]
    public async Task Use_RegistersAllDependencies()
    {
        // Arrange
        var host = Host.CreateDefaultBuilder()
            .UseMongoDbMigrations()
            .ConfigureServices(collection => collection.AddSingleton<IMongoClient>(MongoFixture.Client))
            .Build();

        // Act
        await host.StartAsync();

        // Assert
        var service = host.Services.GetRequiredService<MigrationEngine>();
        service.Should().NotBeNull();

        await host.StopAsync();
    }

    [Test]
    public async Task MigrateMongoDb_WhenCalled_MigratesDatabase()
    {
        // Arrange
        var host = Host.CreateDefaultBuilder()
            .UseMongoDbMigrations(builder => builder
                .WithDatabaseName("default")
                .WithCollectionName("_migrations")
                .WithMigrationAssemblies(typeof(TestMigration).Assembly))
            .ConfigureServices(collection => collection.AddSingleton<IMongoClient>(MongoFixture.Client))
            .Build();

        // Act
        await host.StartAsync();
        await host.MigrateMongoDb();

        // Assert
        var mongoClient = host.Services.GetRequiredService<IMongoClient>();
        var database = mongoClient.GetDatabase("default");
        var migrations = await database.GetCollection<Migration>("_migrations")
            .CountDocumentsAsync(FilterDefinition<Migration>.Empty);

        migrations.Should().Be(1);
        database.GetCollection<Migration>(nameof(TestMigration)).Should().NotBeNull();
        await host.StopAsync();
    }
}