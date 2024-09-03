using FluentAssertions;
using MongoDB.Driver;
using Rapidata.MongoDB.Migrations.Config;
using Rapidata.MongoDB.Migrations.Entities;
using Rapidata.MongoDB.Migrations.Repositories;
using Rapidata.MongoDB.Migrations.Tests.Unit.Builders;

namespace Rapidata.MongoDB.Migrations.Tests.Integration.Repositories;

public class MigrationRepositoryTests
{
    private string CollectionName { get; set; } = null!;
    private MigrationRepository Subject { get; set; } = null!;
    private IMongoCollection<Migration> Collection { get; set; } = null!;
    private IMongoDatabase Database { get; set; } = null!;

    [SetUp]
    public void Setup()
    {
        CollectionName = Guid.NewGuid().ToString();
        var config = new MigrationConfigBuilder().WithCollectionName(CollectionName).Build();

        Database = MongoFixture.Client.GetDatabase("test");
        Subject = new MigrationRepository(Database, config);
        Collection = Database.GetCollection<Migration>(CollectionName);
    }

    [TearDown]
    public void TearDown()
    {
        Database.DropCollection(CollectionName);
    }

    [Test]
    public async Task HasRunningMigrations_WhenNoMigrations_ReturnsFalse()
    {
        // Act
        var result = await Subject.HasRunningMigrations(CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task HasRunningMigrations_WhenMultipleMigrationsWithNoRunning_ReturnsFalse()
    {
        // Arrange
        var migration1 = new MigrationMockBuilder().WithVersion(1).Build();
        var migration2 = new MigrationMockBuilder().WithVersion(2).Build();
        var migration3 = new MigrationMockBuilder().WithVersion(3).Build();

        await Subject.StartMigration(migration1.Object, CancellationToken.None);
        await Subject.StartMigration(migration2.Object, CancellationToken.None);
        await Subject.StartMigration(migration3.Object, CancellationToken.None);

        await Subject.CompleteMigration(migration1.Object, CancellationToken.None);
        await Subject.CompleteMigration(migration2.Object, CancellationToken.None);
        await Subject.FailMigration(migration3.Object, CancellationToken.None);

        // Act
        var result = await Subject.HasRunningMigrations(CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task HasRunningMigrations_WhenMultipleMigrationsWithDifferentStates_ReturnsTrue()
    {
        // Arrange
        var migration1 = new MigrationMockBuilder().WithVersion(1).Build();
        var migration2 = new MigrationMockBuilder().WithVersion(2).Build();
        var migration3 = new MigrationMockBuilder().WithVersion(3).Build();

        await Subject.StartMigration(migration1.Object, CancellationToken.None);
        await Subject.StartMigration(migration2.Object, CancellationToken.None);
        await Subject.StartMigration(migration3.Object, CancellationToken.None);

        await Subject.CompleteMigration(migration1.Object, CancellationToken.None);
        await Subject.FailMigration(migration3.Object, CancellationToken.None);

        // Act
        var result = await Subject.HasRunningMigrations(CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task HasFailedMigrations_WhenNoMigrations_ReturnsFalse()
    {
        // Act
        var result = await Subject.HasFailedMigrations(CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task HasFailedMigrations_WhenMultipleMigrationsWithNoFailed_ReturnsFalse()
    {
        // Arrange
        var migration1 = new MigrationMockBuilder().WithVersion(1).Build();
        var migration2 = new MigrationMockBuilder().WithVersion(2).Build();
        var migration3 = new MigrationMockBuilder().WithVersion(3).Build();

        await Subject.StartMigration(migration1.Object, CancellationToken.None);
        await Subject.StartMigration(migration2.Object, CancellationToken.None);
        await Subject.StartMigration(migration3.Object, CancellationToken.None);

        await Subject.CompleteMigration(migration1.Object, CancellationToken.None);
        await Subject.CompleteMigration(migration2.Object, CancellationToken.None);

        // Act
        var result = await Subject.HasFailedMigrations(CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public async Task HasFailedMigrations_WhenMultipleMigrationsWithDifferentStates_ReturnsTrue()
    {
        // Arrange
        var migration1 = new MigrationMockBuilder().WithVersion(1).Build();
        var migration2 = new MigrationMockBuilder().WithVersion(2).Build();
        var migration3 = new MigrationMockBuilder().WithVersion(3).Build();

        await Subject.StartMigration(migration1.Object, CancellationToken.None);
        await Subject.StartMigration(migration2.Object, CancellationToken.None);
        await Subject.StartMigration(migration3.Object, CancellationToken.None);

        await Subject.CompleteMigration(migration1.Object, CancellationToken.None);
        await Subject.FailMigration(migration3.Object, CancellationToken.None);

        // Act
        var result = await Subject.HasFailedMigrations(CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task GetMigrations_WhenNoMigrations_ReturnsEmpty()
    {
        // Act
        var result = await Subject.GetMigrations(CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetMigrations_WhenSingleMigrations_ReturnsThat()
    {
        // Arrange
        var migration = new MigrationMockBuilder().WithVersion(1).Build();
        await Subject.StartMigration(migration.Object, CancellationToken.None);

        // Act
        var result = await Subject.GetMigrations(CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().HaveCount(1);
        result.Single().Version.Should().Be(1);
    }

    [Test]
    public async Task GetMigrations_WhenMultipleMigrationsWithDifferentStates_ReturnsAllInAnyOrder()
    {
        // Arrange
        var migration1 = new MigrationMockBuilder().WithVersion(1).Build();
        var migration2 = new MigrationMockBuilder().WithVersion(2).Build();
        var migration3 = new MigrationMockBuilder().WithVersion(3).Build();

        await Subject.StartMigration(migration1.Object, CancellationToken.None);
        await Subject.StartMigration(migration2.Object, CancellationToken.None);
        await Subject.StartMigration(migration3.Object, CancellationToken.None);

        await Subject.CompleteMigration(migration1.Object, CancellationToken.None);
        await Subject.FailMigration(migration3.Object, CancellationToken.None);

        // Act
        var result = await Subject.GetMigrations(CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(x => x.Version == 1);
        result.Should().Contain(x => x.Version == 2);
        result.Should().Contain(x => x.Version == 3);
    }

    [Test]
    public async Task GetFailedMigrations_WhenNoMigrations_ReturnsEmpty()
    {
        // Act
        var result = await Subject.GetFailedMigrations(CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetFailedMigrations_WhenMultipleMigrationsWithNoFailed_ReturnsEmpty()
    {
        // Arrange
        var migration1 = new MigrationMockBuilder().WithVersion(1).Build();
        var migration2 = new MigrationMockBuilder().WithVersion(2).Build();
        var migration3 = new MigrationMockBuilder().WithVersion(3).Build();

        await Subject.StartMigration(migration1.Object, CancellationToken.None);
        await Subject.StartMigration(migration2.Object, CancellationToken.None);
        await Subject.StartMigration(migration3.Object, CancellationToken.None);

        await Subject.CompleteMigration(migration1.Object, CancellationToken.None);
        await Subject.CompleteMigration(migration2.Object, CancellationToken.None);

        // Act
        var result = await Subject.GetFailedMigrations(CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetFailedMigrations_WhenMultipleMigrationsWithDifferentStates_ReturnsFailed()
    {
        // Arrange
        var migration1 = new MigrationMockBuilder().WithVersion(1).Build();
        var migration2 = new MigrationMockBuilder().WithVersion(2).Build();
        var migration3 = new MigrationMockBuilder().WithVersion(3).Build();

        await Subject.StartMigration(migration1.Object, CancellationToken.None);
        await Subject.StartMigration(migration2.Object, CancellationToken.None);
        await Subject.StartMigration(migration3.Object, CancellationToken.None);

        await Subject.CompleteMigration(migration1.Object, CancellationToken.None);
        await Subject.FailMigration(migration3.Object, CancellationToken.None);

        // Act
        var result = await Subject.GetFailedMigrations(CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().BeEquivalentTo(new short[] { 3 });
    }

    [Test]
    public async Task StartMigration_WhenMigrationDoesNotExist_ReturnsTrueAndCreatesMigration()
    {
        // Arrange
        var migration = new MigrationMockBuilder().WithVersion(1).Build();

        // Act
        var result = await Subject.StartMigration(migration.Object, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().BeTrue();
        var numberOfMigrations = await Collection.CountDocumentsAsync(FilterDefinition<Migration>.Empty)
            .ConfigureAwait(false);
        numberOfMigrations.Should().Be(1);
    }

    [Test]
    public async Task StartMigration_WhenMigrationExists_ReturnsFalseAndDoesNotCreateMigration()
    {
        // Arrange
        var migration = new MigrationMockBuilder().WithVersion(1).Build();
        await Subject.StartMigration(migration.Object, CancellationToken.None);

        // Act
        var result = await Subject.StartMigration(migration.Object, CancellationToken.None).ConfigureAwait(false);

        // Assert
        result.Should().BeFalse();
        var numberOfMigrations = await Collection.CountDocumentsAsync(FilterDefinition<Migration>.Empty)
            .ConfigureAwait(false);
        numberOfMigrations.Should().Be(1);
    }

    [Test]
    [TestCase(100)]
    public async Task StartMigration_WhenHammered_CreateOnlyOneMigration(int numberOfTasks)
    {
        // Arrange
        var migration = new MigrationMockBuilder().WithVersion(1).Build();

        // Act
        var tasks = new List<Task<bool>>();
        for (var i = 0; i < numberOfTasks; i++)
            tasks.Add(Subject.StartMigration(migration.Object, CancellationToken.None));

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);

        // Assert
        results.Where(x => x).Should().HaveCount(1);
        var numberOfMigrations = await Collection.CountDocumentsAsync(FilterDefinition<Migration>.Empty)
            .ConfigureAwait(false);
        numberOfMigrations.Should().Be(1);
    }

    [Test]
    public async Task CompleteMigration_WhenMigrationDoesNotExist_DoesNothing()
    {
        // Arrange
        var migration = new MigrationMockBuilder().WithVersion(1).Build();

        // Act
        await Subject.CompleteMigration(migration.Object, CancellationToken.None).ConfigureAwait(false);

        // Assert
        var numberOfMigrations = await Collection.CountDocumentsAsync(FilterDefinition<Migration>.Empty)
            .ConfigureAwait(false);
        numberOfMigrations.Should().Be(0);
    }

    [Test]
    public async Task CompleteMigration_WhenMigrationExists_CompletesMigration()
    {
        // Arrange
        var migration = new MigrationMockBuilder().WithVersion(1).Build();
        await Subject.StartMigration(migration.Object, CancellationToken.None);

        // Act
        await Subject.CompleteMigration(migration.Object, CancellationToken.None).ConfigureAwait(false);

        // Assert
        var completedMigration = await Collection.Find(x => x.Version == 1)
            .FirstOrDefaultAsync(CancellationToken.None)
            .ConfigureAwait(false);
        completedMigration.Should().NotBeNull();
        completedMigration!.State.Should().Be(MigrationState.Applied);
    }

    [Test]
    public async Task FailMigration_WhenMigrationDoesNotExist_DoesNothing()
    {
        // Arrange
        var migration = new MigrationMockBuilder().WithVersion(1).Build();

        // Act
        await Subject.FailMigration(migration.Object, CancellationToken.None).ConfigureAwait(false);

        // Assert
        var numberOfMigrations = await Collection.CountDocumentsAsync(FilterDefinition<Migration>.Empty)
            .ConfigureAwait(false);
        numberOfMigrations.Should().Be(0);
    }

    [Test]
    public async Task FailMigration_WhenMigrationExists_FailsMigration()
    {
        // Arrange
        var migration = new MigrationMockBuilder().WithVersion(1).Build();
        await Subject.StartMigration(migration.Object, CancellationToken.None);

        // Act
        await Subject.FailMigration(migration.Object, CancellationToken.None).ConfigureAwait(false);

        // Assert
        var failedMigration = await Collection.Find(x => x.Version == 1)
            .FirstOrDefaultAsync(CancellationToken.None)
            .ConfigureAwait(false);
        failedMigration.Should().NotBeNull();
        failedMigration!.State.Should().Be(MigrationState.Failed);
    }
}