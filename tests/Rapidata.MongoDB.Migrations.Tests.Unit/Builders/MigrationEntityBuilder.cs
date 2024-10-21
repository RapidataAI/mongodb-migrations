using MongoDB.Bson;
using Rapidata.MongoDB.Migrations.Entities;

namespace Rapidata.MongoDB.Migrations.Tests.Unit.Builders;

public class MigrationEntityBuilder
{
    private DateTime _createdAt = DateTime.UtcNow;
    private DateOnly _date = DateOnly.FromDateTime(DateTime.UtcNow);
    private int? _developerId;
    private string _id = ObjectId.GenerateNewId().ToString();
    private string _name = "Test";
    private MigrationState _state = MigrationState.Applied;
    private short _version = 1;

    public Migration Build()
    {
        return new Migration
        {
            Id = _id,
            Name = _name,
            Version = _version,
            State = _state,
            AppliedAt = _createdAt,
            Date = _date,
            DeveloperId = _developerId,
        };
    }

    public MigrationEntityBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public MigrationEntityBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public MigrationEntityBuilder WithVersion(short version)
    {
        _version = version;
        return this;
    }

    public MigrationEntityBuilder WithState(MigrationState state)
    {
        _state = state;
        return this;
    }

    public MigrationEntityBuilder WithCreatedAt(DateTime createdAt)
    {
        _createdAt = createdAt;
        return this;
    }

    public MigrationEntityBuilder WithDate(DateOnly date)
    {
        _date = date;
        return this;
    }

    public MigrationEntityBuilder WithDeveloperId(int? developerId)
    {
        _developerId = developerId;
        return this;
    }
}
