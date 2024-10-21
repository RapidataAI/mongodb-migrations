using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using Moq;
using Rapidata.MongoDB.Migrations.Config;
using Rapidata.MongoDB.Migrations.Contracts;
using Rapidata.MongoDB.Migrations.Core;
using Rapidata.MongoDB.Migrations.Entities;
using Rapidata.MongoDB.Migrations.Providers;
using Rapidata.MongoDB.Migrations.Services;
using Rapidata.MongoDB.Migrations.Tests.Unit.Builders;

namespace Rapidata.MongoDB.Migrations.Tests.Integration.Core;

public class MigrationEngineTests
{
    private const string DefaultDatabaseName = "default";

    private string CollectionName { get; set; } = null!;

    private MigrationEngine Subject { get; set; } = null!;

    private IDictionary<string, IMongoCollection<Migration>> Collections { get; set; } = null!;

    private IMongoCollection<Migration> DefaultCollection => Collections[DefaultDatabaseName];

    private MigrationService MigrationService { get; set; } = null!;

    private Mock<IMigrationResolver> MigrationResolver { get; set; } = null!;

    private void Setup(Action<MigrationConfigBuilder>? configure = null, params IMigration[] migrations)
    {
        CollectionName = Guid.NewGuid().ToString();
        var configBuilder = new MigrationConfigBuilder()
            .WithCollection(CollectionName)
            .WithDatabase("default");

        configure?.Invoke(configBuilder);
        var config = configBuilder.Build();

        MigrationResolver = new Mock<IMigrationResolver>();
        MigrationResolver
            .Setup(x => x.GetMigrations(It.IsAny<IEnumerable<Assembly>>()))
            .Returns(migrations);

        var serviceLogger = new NullLogger<MigrationService>();
        MigrationService = new MigrationService(
            new DefaultMongoClientProvider(MongoFixture.Client),
            config,
            MigrationResolver.Object,
            serviceLogger);
        var logger = new NullLogger<MigrationEngine>();

        Subject = new MigrationEngine(MigrationService, logger);
        Collections = config.MigrationAssembliesPerDatabase.Keys
            .ToDictionary(
                databaseName => databaseName,
                databaseName => MongoFixture.Client.GetDatabase(databaseName).GetCollection<Migration>(CollectionName));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var database in Collections.Keys)
        {
            MongoFixture.Client.DropDatabase(database);
        }
    }

    [Test]
    public async Task Migrate_WhenMigrationGiven_SetsCorrectMetadata()
    {
        // Arrange
        const string name = "The cool migration";
        const short version = 7;
        var migration = new MigrationMockBuilder()
            .WithName(name)
            .WithVersion(version)
            .WithDate(new DateOnly(year: 2024, month: 10, day: 13))
            .Build();
        Setup(configure: null, migration.Object);

        // Act
        await Subject.Migrate(CancellationToken.None);

        // Assert
        var appliedMigration = await DefaultCollection.Find(FilterDefinition<Migration>.Empty).FirstOrDefaultAsync();
        appliedMigration.Version.Should().Be(version);
        appliedMigration.Name.Should().Be(name);
        appliedMigration.AppliedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMilliseconds(100));
        appliedMigration.Date.Should().Be(new DateOnly(year: 2024, month: 10, day: 13));
    }

    [Test]
    public async Task Migrate_WhenSingleMigrationGivenAndNoPreviousMigrations_AppliesMigration()
    {
        // Arrange
        var migration = new MigrationMockBuilder().Build();
        Setup(configure: null, migration.Object);

        // Act
        await Subject.Migrate(CancellationToken.None);

        // Assert
        var appliedMigration = await DefaultCollection.Find(FilterDefinition<Migration>.Empty).FirstAsync();
        appliedMigration.State.Should().Be(MigrationState.Applied);
    }

    [Test]
    public async Task Migrate_WhenSingleMigrationGivenThatFails_MarksMigrationAsFailed()
    {
        // Arrange
        var migration = new MigrationMockBuilder().WithAction((_, _) => throw new Exception()).Build();
        Setup(configure: null, migration.Object);

        // Act
        await Subject.Migrate(CancellationToken.None);

        // Assert
        var appliedMigration = await DefaultCollection.Find(FilterDefinition<Migration>.Empty).FirstAsync();
        appliedMigration.State.Should().Be(MigrationState.Failed);
    }

    [Test]
    [TestCase(100)]
    public async Task Migrate_WhenHammered_AppliesMigrationOnce(int concurrentJobs)
    {
        // Arrange
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50000));
        var migration = new MigrationMockBuilder().Build();
        Setup(configure: null, migration.Object);

        var jobs = new List<Task>();
        for (var i = 0; i < concurrentJobs; i++)
        {
            jobs.Add(Subject.Migrate(cts.Token));
        }

        // Act
        await Task.WhenAll(jobs.ToArray());

        // Assert
        var count = await DefaultCollection.CountDocumentsAsync(
            FilterDefinition<Migration>.Empty,
            cancellationToken: cts.Token);
        count.Should().Be(1);

        migration.Verify(
            m => m.Migrate(It.IsAny<IMongoDatabase>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Migrate_WhenMultipleDatabasesGiven_AppliesMigrationToAll()
    {
        // Arrange
        var migration = new MigrationMockBuilder().Build();
        Setup(
            builder => builder
                .WithMigrationAssembliesForDatabase("db1")
                .WithMigrationAssembliesForDatabase("db2"),
            migration.Object);

        // Act
        await Subject.Migrate(CancellationToken.None);

        // Assert
        var db1Count = await Collections["db1"].CountDocumentsAsync(FilterDefinition<Migration>.Empty);
        var db2Count = await Collections["db2"].CountDocumentsAsync(FilterDefinition<Migration>.Empty);

        db1Count.Should().Be(1);
        db2Count.Should().Be(1);
        Collections.Count.Should().Be(2);
    }

    [Test]
    public async Task GetMigrationsToExecute_OrdersMigrationsByDateThenByDeveloperIdThenByVersion()
    {
        // Arrange
        var migration1 = new MigrationMockBuilder()
            .WithDate(DateOnly.FromDateTime(DateTime.Today.AddDays(-1)))
            .WithDeveloperId(1)
            .WithVersion(1)
            .Build()
            .Object;

        var migration2 = new MigrationMockBuilder()
            .WithDate(DateOnly.FromDateTime(DateTime.Today))
            .WithDeveloperId(1)
            .WithVersion(2)
            .Build()
            .Object;

        var migration3 = new MigrationMockBuilder()
            .WithDate(DateOnly.FromDateTime(DateTime.Today))
            .WithDeveloperId(2)
            .WithVersion(1)
            .Build()
            .Object;

        var migration4 = new MigrationMockBuilder()
            .WithDate(DateOnly.FromDateTime(DateTime.Today))
            .WithDeveloperId(2)
            .WithVersion(2)
            .Build()
            .Object;

        var migration5 = new MigrationMockBuilder()
            .WithDate(DateOnly.FromDateTime(DateTime.Today.AddDays(1)))
            .WithDeveloperId(null)
            .WithVersion(1)
            .Build()
            .Object;

        var migration6 = new MigrationMockBuilder()
            .WithDate(DateOnly.FromDateTime(DateTime.Today.AddDays(1)))
            .WithDeveloperId(null)
            .WithVersion(2)
            .Build()
            .Object;

        var migration7 = new MigrationMockBuilder()
            .WithDate(DateOnly.FromDateTime(DateTime.Today.AddDays(1)))
            .WithDeveloperId(null)
            .WithVersion(3)
            .Build()
            .Object;

        var migration8 = new MigrationMockBuilder()
            .WithDate(DateOnly.FromDateTime(DateTime.Today.AddDays(1)))
            .WithDeveloperId(1)
            .WithVersion(1)
            .Build()
            .Object;

        var migration9 = new MigrationMockBuilder()
            .WithDate(DateOnly.FromDateTime(DateTime.Today.AddDays(1)))
            .WithDeveloperId(1)
            .WithVersion(2)
            .Build()
            .Object;

        var migrations = new List<IMigration>
        {
            migration3,
            migration2,
            migration7,
            migration4,
            migration6,
            migration9,
            migration1,
            migration5,
            migration8,
        };

        Setup(configure: null, migrations.ToArray());

        // Act
        var result = await MigrationService.GetMigrationsToExecutePerDatabase(CancellationToken.None);

        // Assert
        var totalMigrations = result.SelectMany(x => x.Value).ToList();
        totalMigrations.Should()
            .BeEquivalentTo(
                new List<IMigration>
                {
                    migration1,
                    migration2,
                    migration3,
                    migration4,
                    migration5,
                    migration6,
                    migration7,
                    migration8,
                    migration9,
                },
                options => options.WithStrictOrdering());
    }

    [Test]
    public async Task Migrate_WhenTwoMigrationsAndOneAlreadyApplied_AppliesOnlyOne()
    {
        // Arrange
        var migration1 = new MigrationMockBuilder().Build();
        Setup(configure: null, migration1.Object);
        await Subject.Migrate(CancellationToken.None);

        var migration2 = new MigrationMockBuilder().WithVersion(2).Build();
        MigrationResolver.Setup(x => x.GetMigrations(It.IsAny<IEnumerable<Assembly>>()))
            .Returns([migration1.Object, migration2.Object]);

        // Act
        var migrationsToExecute = await MigrationService.GetMigrationsToExecutePerDatabase(CancellationToken.None);
        await Subject.Migrate(CancellationToken.None);

        // Assert
        migrationsToExecute.SelectMany(x => x.Value).Should().HaveCount(1);
        var count = await DefaultCollection.CountDocumentsAsync(FilterDefinition<Migration>.Empty);
        count.Should().Be(2);
    }
}
