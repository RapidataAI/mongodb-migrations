using MongoDB.Driver;

namespace Rapidata.MongoDB.Migrations.Providers;

public interface IMongoClientProvider
{
    IMongoClient GetClient();
}
