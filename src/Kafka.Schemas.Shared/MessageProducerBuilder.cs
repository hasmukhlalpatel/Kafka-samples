using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Kafka.Schemas.Shared.Serialization;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Kafka.Schemas.Shared;

public class MessageProducerBuilder<TKey, TValue> : IDisposable, 
    IMessageProducerBuilder<TKey, TValue>
    where TValue : class
{
    private readonly IProducer<TKey, TValue> _producer = null;

    private readonly CachedSchemaRegistryClient? schemaRegistryClient = null;

    private readonly JsonSerializerConfig _jsonSerializerConfig = new JsonSerializerConfig
    {
        AutoRegisterSchemas = false, // Set this back to true for auto-registration
        UseLatestVersion = true,
        LatestCompatibilityStrict = true,
        Validate = false, // Set this back to true for validation
    };
    private readonly IAsyncSerializer<TValue> _serializer;
    private readonly ILogger<MessageProducerBuilder<TKey, TValue>> _logger;

    internal MessageProducerBuilder(ILogger<MessageProducerBuilder<TKey, TValue>> logger)
    {
        var producerConfig = new ProducerConfig { BootstrapServers = KafkaConfig.Default.BootstrapServers };

        var config = new SchemaRegistryConfig
        {
            Url = KafkaConfig.Default.SchemaRegistryUrl
        };

        var schemaRegistry = new CachedSchemaRegistryClient(config);
        var jsonSerializer = new JsonSerializer<TValue>(schemaRegistry, _jsonSerializerConfig);

        _producer = InitializeProducer(producerConfig, jsonSerializer);
        _logger = logger;
    }

    public MessageProducerBuilder(ProducerConfig producerConfig,
        SchemaRegistryConfig config,
        ILogger<MessageProducerBuilder<TKey, TValue>> logger,
        JsonSerializerConfig? jsonSerializerConfig = null)
    {
        jsonSerializerConfig ??= _jsonSerializerConfig;
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        _logger = logger;

        schemaRegistryClient = new CachedSchemaRegistryClient(config);
        _serializer = new JsonSerializer<TValue>(schemaRegistryClient, jsonSerializerConfig);

        _producer = InitializeProducer(producerConfig, _serializer);
    }

    public MessageProducerBuilder(ProducerConfig producerConfig,
        IAsyncSerializer<TValue> serializer,
        ILogger<MessageProducerBuilder<TKey, TValue>> logger)
    {
        _producer = InitializeProducer(producerConfig, serializer);
        _serializer = serializer;
        _logger = logger;
    }

    /// <summary>
    /// Initializes a new instance of the MessageProducerBuilder class with the specified ProducerConfig.
    /// No schema registry client is used, and a custom serializer is created for TValue.
    /// </summary>
    /// <param name="producerConfig"></param>
    /// <param name="logger"></param>
    public MessageProducerBuilder(ProducerConfig producerConfig, ILogger<MessageProducerBuilder<TKey, TValue>> logger = null)
    {
        _serializer = new CustomSerializer<TValue>();
        _producer = InitializeProducer(producerConfig, _serializer);
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
