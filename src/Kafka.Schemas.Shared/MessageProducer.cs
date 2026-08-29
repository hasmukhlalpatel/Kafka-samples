using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Kafka.Schemas.Shared.Extensions;
using Kafka.Schemas.Shared.Serialization;
using Microsoft.Extensions.Logging;
using Observability.Shared;
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
        _producer = InitializeProducer(producerConfig, null, null);
        _logger = logger;
    }

    public MessageProducer(ProducerConfig producerConfig, IAsyncSerializer<TValue>? serializer, ILogger<MessageProducer<TKey, TValue>> logger)
    {
        _producer = InitializeProducer(producerConfig, serializer, null);
        _logger = logger;
    }
    public MessageProducer(ProducerConfig producerConfig, ISerializer<TValue> serializer, ILogger<MessageProducer<TKey, TValue>> logger)
    {
        _producer = InitializeProducer(producerConfig, null, serializer);
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

        _producer = InitializeProducer(producerConfig, serializer, null);
    }

    private IProducer<TKey, TValue> InitializeProducer(ProducerConfig producerConfig,
        IAsyncSerializer<TValue>? asyncSerializer,
        ISerializer<TValue>? serializer)
    {
        if (_producer == null)
        {
            if (serializer == null && asyncSerializer == null && !DefaultSerializerConfig.TryGetDefaultSerializer(typeof(TValue), out _))
            {
                throw new ArgumentNullException(nameof(serializer));
            }

            var builder = new ProducerBuilder<TKey, TValue>(producerConfig);

            if (asyncSerializer != null)
            {
                builder.SetValueSerializer(asyncSerializer);
            }
            else if (serializer != null)
            {
                builder.SetValueSerializer(serializer);
            }

            return builder.Build();
        }
        return _producer;
    }

    private ActivitySourceProvider activitySource = new ActivitySourceProvider("Kafka.MessageProducer");

    public async Task ProduceAsync(string topic, Message<TKey, TValue> message, CancellationToken cancellationToken = default)
    {
        var activity = activitySource.StartProducerActivity(topic);
        try
        {
            message.Headers ??= new Headers();
            message.Headers.AddHeader(LogicalCallContext.Constants.XCorrelationId, ApplicationContextScope.Current.CorrelationId);
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
