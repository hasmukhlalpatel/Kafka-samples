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
    private readonly IDeserializer<TValue> _deserializer;

    private readonly JsonSerializerConfig _jsonSerializerConfig = new JsonSerializerConfig
    {
        AutoRegisterSchemas = false, // Set this back to true for auto-registration
        UseLatestVersion = true,
        LatestCompatibilityStrict = true,
        Validate = false, // Set this back to true for validation
    };

    public MessageConsumer(ConsumerConfig consumerConfig, IDeserializer<TValue> deserializer, ILogger<MessageConsumer<TKey, TValue>> logger)
    {
        _consumerConfig = consumerConfig;
        _logger = logger;
        _deserializer = deserializer;
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

    private IConsumer<TKey, TValue> BuildConsumer()
    {
        return new ConsumerBuilder<TKey, TValue>(_consumerConfig)
            .SetValueDeserializer(_deserializer)
            .Build();
    }

    public void StartConsuming(string topic, Func<TKey, TValue, ConsumeStatus> consumerFactory, CancellationToken cancellationToken = default)
    {
        StartConsuming(topic, _consumerConfig.GroupId, consumerFactory, cancellationToken);
    }

    public void StartConsuming(string topic, string groupId, Func<TKey, TValue, ConsumeStatus> consumerFactory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(topic, nameof(topic));
        ArgumentException.ThrowIfNullOrEmpty(groupId, nameof(groupId));

        _logger.BeginScope(new Dictionary<string, object>
        {
            ["Topic"] = topic,
            ["GroupId"] = groupId
        });

        using (var consumer = BuildConsumer())
        {
            _logger.LogInformation($"Starting consumer for topic '{topic}' with group ID '{groupId}'");
            consumer.Subscribe(topic);
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(cancellationToken);
                        _logger.LogInformation($"Processing Consumed message '{consumeResult.Message.Value}' at: '{consumeResult.TopicPartitionOffset}'.");
                        var consumeStatus = consumerFactory(consumeResult.Message.Key, consumeResult.Message.Value);
                        _logger.LogInformation($"Consumed message '{consumeResult.Message.Value}' at: '{consumeResult.TopicPartitionOffset}'.");
                    }
                    catch (ConsumeException e)
                    {
                        _logger.LogError($"Error consuming message: {e.Error.Reason}");
                    }
                    finally
                    {
                        consumer.Commit();
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
}
