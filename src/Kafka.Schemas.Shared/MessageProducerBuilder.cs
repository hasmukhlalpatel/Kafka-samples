using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using System.Text;

namespace Kafka.Schemas.Shared
{
    public class MessageProducerBuilder<TKey, TValue> : IDisposable, 
        IMessageProducerBuilder<TKey, TValue>
        where TValue : class
    {
        private readonly IProducer<TKey, TValue> _producer;

        public MessageProducerBuilder()
        {
            var producerConfig = new ProducerConfig { BootstrapServers = KafkaConfig.Default.BootstrapServers };

            var config = new SchemaRegistryConfig
            {
                Url = KafkaConfig.Default.SchemaRegistryUrl
            };

            var jsonSerializerConfig = new JsonSerializerConfig
            {
                AutoRegisterSchemas = false, // Set this back to true for auto-registration
                UseLatestVersion = true,
                LatestCompatibilityStrict = true,
            };
        }

        public MessageProducerBuilder(ProducerConfig producerConfig,
            SchemaRegistryConfig config, JsonSerializerConfig jsonSerializerConfig)
        {
            var schemaRegistry = new CachedSchemaRegistryClient(config);
            _producer = new ProducerBuilder<TKey, TValue>(producerConfig)
                        .SetValueSerializer(new JsonSerializer<TValue>(schemaRegistry, jsonSerializerConfig))
                        .Build();
        }

        public async Task ProduceAsync(string topic, TKey key, TValue value, IReadOnlyDictionary<string, string> headers,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var messageHeaders = new Headers();
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        messageHeaders.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));
                    }
                }
                var message = new Message<TKey, TValue>
                {
                    Key = key,
                    Value = value,
                    Headers = messageHeaders
                };
                var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken);
                Console.WriteLine($"Message delivered to {deliveryResult.TopicPartitionOffset}");
            }
            catch (ProduceException<TKey, TValue> e)
            {
                Console.WriteLine($"Delivery failed: {e.Error.Reason}");
            }
        }
        public void Dispose()
        {
            _producer?.Dispose();
        }
    }
}
