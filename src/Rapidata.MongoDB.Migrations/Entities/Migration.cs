using Rapidata.MongoDB.Migrations.Contracts;

namespace Rapidata.MongoDB.Migrations.Entities;

public sealed class Migration : IBaseMigration
{
    public string Id { get; init; } = null!;

    public required string Name { get; init; }

    public required MigrationState State { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required DateOnly Date { get; init; }

    public required int? DeveloperId { get; init; }

    public required short Version { get; init; }
}