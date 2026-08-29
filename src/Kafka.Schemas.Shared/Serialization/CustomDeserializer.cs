using Confluent.Kafka;
using Newtonsoft.Json;
using System.Text;

namespace Kafka.Schemas.Shared.Serialization;

public class CustomDeserializer<TValue> : IDeserializer<TValue>
{
    public TValue Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull || data.IsEmpty || data.Length == 0)
        {
            return default;
        }

        var jsonPosition = 0;
        if (data[0] == 0) // Check for magic byte
        {
            var schemaIdBytes = data.Slice(1, 4).ToArray();
            jsonPosition = 5;
        }
        var jsonText = Encoding.UTF8.GetString(data.Slice(jsonPosition).ToArray());

        return JsonConvert.DeserializeObject<TValue>(jsonText);
    }
}