using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Kafka.Schemas.Shared.Serialization;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Kafka.Schemas.Shared;

public class MessageProducer<TKey, TValue> : IMessageProducer<TKey, TValue>
    where TValue : class
{
    private readonly IProducer<TKey, TValue> _producer = null;

    private readonly CachedSchemaRegistryClient? schemaRegistryClient = null;


    private readonly ILogger<MessageProducer<TKey, TValue>> _logger;

    internal MessageProducer(ILogger<MessageProducer<TKey, TValue>> logger)
    {
        var producerConfig = new ProducerConfig { BootstrapServers = KafkaConfig.Default.BootstrapServers };

        var config = new SchemaRegistryConfig
        {
            Url = KafkaConfig.Default.SchemaRegistryUrl
        };

        var schemaRegistry = new CachedSchemaRegistryClient(config);
        var jsonSerializer = new JsonSerializer<TValue>(schemaRegistry, DefaultSerializerConfig.SerializerConfig);

        _producer = InitializeProducer(producerConfig, jsonSerializer);
        _logger = logger;
    }

    public MessageProducer(ProducerConfig producerConfig,
        SchemaRegistryConfig config,
        ILogger<MessageProducer<TKey, TValue>> logger,
        JsonSerializerConfig? jsonSerializerConfig = null)
    {
        jsonSerializerConfig ??= DefaultSerializerConfig.SerializerConfig;
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        _logger = logger;

        schemaRegistryClient = new CachedSchemaRegistryClient(config);
        var serializer = new JsonSerializer<TValue>(schemaRegistryClient, jsonSerializerConfig);

        _producer = InitializeProducer(producerConfig, serializer);
    }

    public MessageProducer(ProducerConfig producerConfig, IAsyncSerializer<TValue> serializer, ILogger<MessageProducer<TKey, TValue>> logger)
    {
        _producer = InitializeProducer(producerConfig, serializer);
        _logger = logger;
    }

    /// <summary>
    /// Initializes a new instance of the MessageProducer class with the specified ProducerConfig.
    /// No schema registry client is used, and a custom serializer is created for TValue.
    /// </summary>
    /// <param name="producerConfig"></param>
    /// <param name="logger"></param>
    public MessageProducer(ProducerConfig producerConfig, ILogger<MessageProducer<TKey, TValue>> logger)
    {
        var serializer = new CustomSerializer<TValue>();
        _producer = InitializeProducer(producerConfig, serializer);
    }

    private IProducer<TKey, TValue> InitializeProducer(ProducerConfig producerConfig, IAsyncSerializer<TValue> serializer)
    {
        if (_producer == null)
        {
            ArgumentNullException.ThrowIfNull(serializer, nameof(serializer));

            return new ProducerBuilder<TKey, TValue>(producerConfig)
                .SetValueSerializer(serializer)
                    .Build();
        }
        return _producer;
    }

    public async Task ProduceAsync(string topic, Message<TKey, TValue> message, CancellationToken cancellationToken = default)
    {
        try
        {
            var deliveryResult = await _producer.ProduceAsync(topic, message, cancellationToken);
            _logger.LogInformation($"Message delivered to {deliveryResult.TopicPartitionOffset}");
        }
        catch (ProduceException<TKey, TValue> e)
        {
            _logger.LogError($"Delivery failed: {e.Error.Reason}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while producing the message: {ex.Message}");
        }
    }
    
    public async Task ProduceAsync(string topic, TKey key, TValue value, CancellationToken cancellationToken = default)
    {
        var message = new Message<TKey, TValue>
        {
            Key = key,
            Value = value,
            Headers = new Headers()
        };
        await ProduceAsync(topic, message, cancellationToken);
    }

    public async Task ProduceAsync(string topic, TKey key, TValue value, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default)
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
        await ProduceAsync(topic, message, cancellationToken);
    }

    public void Dispose()
    {
        schemaRegistryClient?.Dispose();
        _producer?.Dispose();
    }
}
