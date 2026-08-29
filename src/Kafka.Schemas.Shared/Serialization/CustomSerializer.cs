using Confluent.Kafka;
using Newtonsoft.Json;
using NJsonSchema;
using NJsonSchema.NewtonsoftJson.Generation;
using System.Text;

namespace Kafka.Schemas.Shared.Serialization;

public class CustomSerializer<TValue> : IAsyncSerializer<TValue>
{
    private readonly NewtonsoftJsonSchemaGeneratorSettings? _jsonSchemaGeneratorSettings;
    private JsonSerializerSettings? jsonSchemaGeneratorSettingsSerializerSettings => _jsonSchemaGeneratorSettings?.SerializerSettings;
    private readonly JsonSchema _schema;

    public string schemaText => _schema.ToJson();
    public JsonSchema schema => _schema;

    public CustomSerializer(NewtonsoftJsonSchemaGeneratorSettings? jsonSchemaGeneratorSettings = null)
    {
        _schema = ((jsonSchemaGeneratorSettings == null) 
            ? NewtonsoftJsonSchemaGenerator.FromType<TValue>() 
            : NewtonsoftJsonSchemaGenerator.FromType<TValue>(jsonSchemaGeneratorSettings));

        _jsonSchemaGeneratorSettings = jsonSchemaGeneratorSettings;
    }

    public async Task<byte[]> SerializeAsync(TValue data, SerializationContext context)
    {
        if (data == null)
        {
            return default!;
        }

        string jsonText = JsonConvert.SerializeObject(data, jsonSchemaGeneratorSettingsSerializerSettings);
        using (var memoryStream = new MemoryStream())
        {
            using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
            {
                streamWriter.Write(0);
                await streamWriter.WriteAsync(jsonText);
                await streamWriter.FlushAsync();
                return memoryStream.ToArray();
            }
        }

        //return Encoding.UTF8.GetBytes(jsonText);
    }
}