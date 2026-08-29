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

    public MessageProducer(ProducerConfig producerConfig, ILogger<MessageProducer<TKey, TValue>> logger)
    {
        _producer = InitializeProducerWithAsyncSerializer(producerConfig, null);
        _logger = logger;
    }

    public MessageProducer(ProducerConfig producerConfig, IAsyncSerializer<TValue>? serializer, ILogger<MessageProducer<TKey, TValue>> logger)
    {
        _producer = InitializeProducerWithAsyncSerializer(producerConfig, serializer);
        _logger = logger;
    }
    public MessageProducer(ProducerConfig producerConfig, ISerializer<TValue> serializer, ILogger<MessageProducer<TKey, TValue>> logger)
    {
        _producer = InitializeProducerWithSerializer(producerConfig, serializer);
        _logger = logger;
    }

    /// <summary>
    /// With schema registry client and json serializer
    /// </summary>
    /// <param name="producerConfig"></param>
    /// <param name="config"></param>
    /// <param name="logger"></param>
    /// <param name="jsonSerializerConfig"></param>
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

        _producer = InitializeProducerWithAsyncSerializer(producerConfig, serializer);
    }

    private IProducer<TKey, TValue> InitializeProducerWithAsyncSerializer(ProducerConfig producerConfig, IAsyncSerializer<TValue>? serializer)
    {
        if (_producer == null)
        {
            if(serializer == null && !DefaultSerializerConfig.TryGetDefaultSerializer(typeof(TValue), out _))
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            return new ProducerBuilder<TKey, TValue>(producerConfig)
                .SetValueSerializer(serializer)
                    .Build();
        }
        return _producer;
    }

    private IProducer<TKey, TValue> InitializeProducerWithSerializer(ProducerConfig producerConfig, ISerializer<TValue>? serializer)
    {
        if (_producer == null)
        {
            if (serializer == null && !DefaultSerializerConfig.TryGetDefaultSerializer(typeof(TValue), out _))
            {
                throw new ArgumentNullException(nameof(serializer));
            }

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
