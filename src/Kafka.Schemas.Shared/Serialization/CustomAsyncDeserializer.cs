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
        if (isNull || data.IsEmpty)
        {
            return default;
        }

        using (var memoryStream = new MemoryStream(data.ToArray()))
        {
            using (var streamReader = new StreamReader(memoryStream, Encoding.UTF8))
            {
                // Skip the first byte (magic byte)
                var magicBytes = new char[4];
                var magicByte = streamReader.Read(magicBytes, 0, 4);
                string jsonText = await streamReader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<TValue>(jsonText, jsonSchemaGeneratorSettingsSerializerSettings);
            }
        }
    }
}
