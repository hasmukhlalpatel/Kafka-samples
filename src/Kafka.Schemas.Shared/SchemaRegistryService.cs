using Confluent.SchemaRegistry;

namespace Kafka.Schemas.Shared
{
    public class SchemaRegistryService : IDisposable, ISchemaRegistryService
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
        public async Task<int> RegisterSchemaAsync(string topicName, Schema schema)
        {
            string subject = $"{topicName}-value";
            try
            {
                return await _schemaRegistryClient.RegisterSchemaAsync(subject, schema);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering schema: {ex.Message}");
                throw;
            }
        }
        public async Task<int> RegisterSchemaAsync(string topicName, string schemaJson)
        {
            var schema = new Confluent.SchemaRegistry.Schema(schemaJson, SchemaType.Json);
            return await RegisterSchemaAsync(topicName, schema);
        }

        public void Dispose()
        {
            _schemaRegistryClient?.Dispose();
        }
    }
}
