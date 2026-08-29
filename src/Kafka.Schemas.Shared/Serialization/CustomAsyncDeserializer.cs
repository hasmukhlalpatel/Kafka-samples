using Confluent.Kafka;
using Newtonsoft.Json;
using NJsonSchema;
using NJsonSchema.NewtonsoftJson.Generation;
using System.Text;

namespace Kafka.Schemas.Shared.Serialization;

public class CustomAsyncDeserializer<TValue> : IAsyncDeserializer<TValue>
{
    private readonly NewtonsoftJsonSchemaGeneratorSettings? _jsonSchemaGeneratorSettings;
    private JsonSerializerSettings? jsonSchemaGeneratorSettingsSerializerSettings => _jsonSchemaGeneratorSettings?.SerializerSettings;
    private readonly JsonSchema _schema;
    public string schemaText => _schema.ToJson();
    public JsonSchema schema => _schema;
    public CustomAsyncDeserializer(NewtonsoftJsonSchemaGeneratorSettings? jsonSchemaGeneratorSettings = null)
    {
        _schema = ((jsonSchemaGeneratorSettings == null)
            ? NewtonsoftJsonSchemaGenerator.FromType<TValue>()
            : NewtonsoftJsonSchemaGenerator.FromType<TValue>(jsonSchemaGeneratorSettings));
        _jsonSchemaGeneratorSettings = jsonSchemaGeneratorSettings;
    }

    public async Task<TValue> DeserializeAsync(ReadOnlyMemory<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull || data.IsEmpty || data.Length == 0)
        {
            return default;
        }

        var jsonPosition = 0;
        if (data.Span[0] == 0) // Check for magic byte
        {
            var schemaIdBytes = data.Slice(1, 4).ToArray();
            jsonPosition = 5;
        }
        var jsonText = Encoding.UTF8.GetString(data.Slice(jsonPosition).ToArray());

        return JsonConvert.DeserializeObject<TValue>(jsonText, jsonSchemaGeneratorSettingsSerializerSettings);
    }
}
