using MongoDB.Driver;

namespace Rapidata.MongoDB.Migrations.Providers;

public class DefaultMongoClientProvider : IMongoClientProvider
{
    private readonly IMongoClient _mongoClient;

    public DefaultMongoClientProvider(IMongoClient mongoClient)
    {
        _mongoClient = mongoClient;
    }
    
    public IMongoClient GetClient() => _mongoClient;
}
