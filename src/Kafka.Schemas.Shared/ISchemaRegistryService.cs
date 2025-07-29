using Confluent.SchemaRegistry;

namespace Kafka.Schemas.Shared
{
    public interface ISchemaRegistryService: IDisposable
    {
        Task<int> RegisterSchemaAsync(string topicName, Schema schema);
        Task<int> RegisterSchemaAsync(string topicName, string schemaJson);
    }
}