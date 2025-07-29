using Confluent.SchemaRegistry;

namespace Kafka.Schemas.Shared
{
    public class SchemaRegistryService : IDisposable
    {
        private readonly CachedSchemaRegistryClient _schemaRegistryClient;

        public SchemaRegistryService()
        {
            var config = new SchemaRegistryConfig
            {
                Url = KafkaConfig.Default.SchemaRegistryUrl
            };
            _schemaRegistryClient = new CachedSchemaRegistryClient(config);
        }
        public SchemaRegistryService(SchemaRegistryConfig config)
        {
            _schemaRegistryClient = new CachedSchemaRegistryClient(config);
        }

        public void Dispose()
        {
            _schemaRegistryClient?.Dispose();
        }
    }
}
