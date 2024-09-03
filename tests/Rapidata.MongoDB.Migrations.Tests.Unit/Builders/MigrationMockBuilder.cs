using MongoDB.Driver;
using Moq;
using Rapidata.MongoDB.Migrations.Contracts;

namespace Rapidata.MongoDB.Migrations.Tests.Unit.Builders;

public class MigrationMockBuilder
{
    private DateOnly _date = DateOnly.FromDateTime(DateTime.Today);
    private int? _developerId;
    private Func<IMongoDatabase, CancellationToken, Task> _migrate = (_, _) => Task.CompletedTask;
    private string _name = "Test";
    private short _version = 1;

    public Mock<IMigration> Build()
    {
        var mock = new Mock<IMigration>();
        mock.SetupGet(migration => migration.Version).Returns(_version);
        mock.SetupGet(migration => migration.Name).Returns(_name);
        mock.SetupGet(migration => migration.DeveloperId).Returns(_developerId);
        mock.SetupGet(migration => migration.Date).Returns(_date);
        mock.Setup(migration => migration.Migrate(It.IsAny<IMongoDatabase>(), It.IsAny<CancellationToken>()))
            .Returns((IMongoDatabase database, CancellationToken ct) => _migrate(database, ct));

        return mock;
    }

    public MigrationMockBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public MigrationMockBuilder WithVersion(short version)
    {
        _version = version;
        return this;
    }

    public MigrationMockBuilder WithAction(Func<IMongoDatabase, CancellationToken, Task> migrate)
    {
        _migrate = migrate;
        return this;
    }

    public MigrationMockBuilder WithDeveloperId(int? developerId)
    {
        _developerId = developerId;
        return this;
    }

    public MigrationMockBuilder WithDate(DateOnly date)
    {
        _date = date;
        return this;
    }
}
