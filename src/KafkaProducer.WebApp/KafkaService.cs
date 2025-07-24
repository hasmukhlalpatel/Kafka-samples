using Confluent.Kafka;
using Confluent.SchemaRegistry;

namespace KafkaProducerWebApp
{
    public class KafkaService
    {
        private readonly ProducerConfig _producerConfig;
        private readonly ConsumerConfig _consumerConfig;
        private readonly SchemaRegistryConfig _schemaRegistryConfig;

        public KafkaService(ProducerConfig producerConfig, ConsumerConfig consumerConfig, SchemaRegistryConfig schemaRegistryConfig)
        {
            _producerConfig = producerConfig;
            _consumerConfig = consumerConfig;
            _schemaRegistryConfig = schemaRegistryConfig;
        }

        public async Task<DeliveryResult<string, string>> ProduceMessage(string topic, string key, string value)
        {
            using var producer = new ProducerBuilder<string, string>(_producerConfig).Build();
            return await producer.ProduceAsync(topic, new Message<string, string> { Key = key, Value = value });
        }

        public string ConsumeMessage(string topic)
        {
            using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
            consumer.Subscribe(topic);
            try
            {
                var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10)); // Wait for 10 seconds
                return consumeResult?.Message?.Value;
            }
            catch (ConsumeException ex)
            {
                Console.WriteLine($"Error consuming message: {ex.Error.Reason}");
                return null;
            }
            finally
            {
                consumer.Unsubscribe();
            }
        }

        public async Task<int> RegisterSchema(string subject, string schema)
        {
            using var schemaRegistryClient = new CachedSchemaRegistryClient(_schemaRegistryConfig);
            return await schemaRegistryClient.RegisterSchemaAsync(subject, schema);
        }
    }
}

