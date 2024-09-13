using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Rapidata.MongoDB.Migrations.Entities;

namespace Rapidata.MongoDB.Migrations.Configuration;

public static class EntityConfiguration
{
    public static void ConfigureMigration()
    {
        BsonClassMap.TryRegisterClassMap<Migration>(classMap =>
        {
            classMap.AutoMap();

            classMap.MapProperty(migration => migration.Id)
                .SetSerializer(new StringSerializer(BsonType.ObjectId));

            classMap.MapProperty(migration => migration.State)
                .SetSerializer(new EnumSerializer<MigrationState>(BsonType.String));
        });
    }
}
