using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Kafka.Schemas.Shared.Serialization;
using Microsoft.Extensions.Logging;

namespace Kafka.Schemas.Shared;

public class MessageConsumer<TKey, TValue> : IMessageConsumer<TKey, TValue>
    where TValue : class
{
    private readonly ConsumerConfig _consumerConfig;
    private readonly ILogger<MessageConsumer<TKey, TValue>> _logger;
    private readonly CachedSchemaRegistryClient? schemaRegistryClient;
    private IDeserializer<TValue>? _deserializer;

    private readonly JsonSerializerConfig _jsonSerializerConfig = new JsonSerializerConfig
    {
        AutoRegisterSchemas = false, // Set this back to true for auto-registration
        UseLatestVersion = true,
        LatestCompatibilityStrict = true,
        Validate = false, // Set this back to true for validation
    };

    public MessageConsumer(ConsumerConfig consumerConfig, IDeserializer<TValue>? deserializer, ILogger<MessageConsumer<TKey, TValue>> logger)
    {
        _consumerConfig = consumerConfig;
        _logger = logger;
        _deserializer = deserializer;
    }
    public MessageConsumer(ConsumerConfig consumerConfig, IAsyncDeserializer<TValue>? asyncDeserializer, ILogger<MessageConsumer<TKey, TValue>> logger)
    {
        _consumerConfig = consumerConfig;
        _logger = logger;
        _deserializer = asyncDeserializer != null ? asyncDeserializer.AsSyncOverAsync() : null;
    }

    public MessageConsumer(ConsumerConfig consumerConfig, ILogger<MessageConsumer<TKey, TValue>> logger)
    {
        _consumerConfig = consumerConfig;
        _logger = logger;
        _deserializer = new CustomDeserializer<TValue>();
    }

    public MessageConsumer(ConsumerConfig consumerConfig,
        SchemaRegistryConfig config,
        ILogger<MessageConsumer<TKey, TValue>> logger,
        JsonSerializerConfig? jsonSerializerConfig = null)
    {
        _consumerConfig = consumerConfig;
        jsonSerializerConfig ??= _jsonSerializerConfig;
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        _logger = logger;

        schemaRegistryClient = new CachedSchemaRegistryClient(config);
        _deserializer = new JsonDeserializer<TValue>(schemaRegistryClient, jsonSerializerConfig).AsSyncOverAsync();
    }

    private IConsumer<TKey, byte[]> BuildConsumer()
    {
        return new ConsumerBuilder<TKey, byte[]>(_consumerConfig).Build();
    }

    public async Task StartConsumingAsync(string topic, Func<TKey, TValue, Task<ConsumeStatus>> consumerFactory, CancellationToken cancellationToken = default)
    {
        await StartConsumingAsync(topic, _consumerConfig.GroupId, consumerFactory, cancellationToken);
    }

    public async Task StartConsumingAsync(string topic, string groupId, Func<TKey, TValue, Task<ConsumeStatus>> consumerFactory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(topic, nameof(topic));
        ArgumentException.ThrowIfNullOrEmpty(groupId, nameof(groupId));

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Topic"] = topic,
            ["GroupId"] = groupId
        });

        _deserializer ??= (IDeserializer<TValue>)DefaultSerializerConfig.GetDefaultSerializer(typeof(TValue));

        using (var consumer = BuildConsumer())
        {
            _logger.LogInformation($"Starting consumer for topic '{topic}' with group ID '{groupId}'");
            consumer.Subscribe(topic);
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(cancellationToken);
                    try
                    {
                        TValue deserializedValue = DesiriliseValue(consumeResult);

                        _logger.LogInformation($"Processing Consumed message at: '{consumeResult.TopicPartitionOffset}'.");
                        var consumeStatus = await consumerFactory(consumeResult.Message.Key, deserializedValue);
                        _logger.LogInformation($"Consumed message at: '{consumeResult.TopicPartitionOffset}'.");
                    }
                    catch (ConsumeException e)
                    {
                        _logger.LogError($"Error consuming message: {e.Error.Reason}");
                    }
                    finally
                    {
                        consumer.Commit(consumeResult);
                        consumer.StoreOffset(consumeResult);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consumer operation canceled.");
            }
            finally
            {
                consumer.Close();
            }
        }
    }

    private TValue DesiriliseValue(ConsumeResult<TKey, byte[]> consumeResult)
    {
        if(typeof(TValue) == typeof(byte[]))
        {
            return (TValue)(object)consumeResult.Message.Value;
        }

        return _deserializer.Deserialize(consumeResult.Message.Value, false, new SerializationContext(MessageComponentType.Value, consumeResult.Topic));
    }
}
