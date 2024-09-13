using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Rapidata.MongoDB.Migrations.Utils;

public class DateOnlySerializer : StructSerializerBase<DateOnly>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value)
    {
        var dateTime = value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        context.Writer.WriteDateTime(BsonUtils.ToMillisecondsSinceEpoch(dateTime));
    }

    public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var ticks = context.Reader.ReadDateTime();
        return DateOnly.FromDateTime(BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(ticks));
    }
}
