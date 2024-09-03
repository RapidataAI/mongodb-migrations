namespace Rapidata.MongoDB.Migrations.Contracts;

public interface IBaseMigration
{
    public DateOnly Date { get; }

    public int? DeveloperId => null;

    public short Version { get; }
}