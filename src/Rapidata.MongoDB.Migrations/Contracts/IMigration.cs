using JetBrains.Annotations;
using MongoDB.Driver;

namespace Rapidata.MongoDB.Migrations.Contracts;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface IMigration : IBaseMigration
{
    string Name { get; }

    Task Migrate(IMongoDatabase database, CancellationToken cancellationToken);
}
