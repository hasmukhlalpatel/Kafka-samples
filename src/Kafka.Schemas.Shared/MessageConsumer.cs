using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Kafka.Schemas.Shared.Extensions;
using Kafka.Schemas.Shared.Serialization;
using Microsoft.Extensions.Logging;
using Observability.Shared;

namespace Kafka.Schemas.Shared;

public class MessageConsumer<TKey, TValue> : IMessageConsumer<TKey, TValue>
    where TValue : class
{
    private readonly ConsumerConfig _consumerConfig;
    private readonly ILogger<MessageConsumer<TKey, TValue>> _logger;
    private readonly CachedSchemaRegistryClient? schemaRegistryClient;
    private IDeserializer<TValue>? _deserializer;
    private readonly IDeadLetterMessageProducer<TKey, byte[]> _dlqProducer;

    private readonly JsonSerializerConfig _jsonSerializerConfig = new JsonSerializerConfig
    {
        AutoRegisterSchemas = false, // Set this back to true for auto-registration
        UseLatestVersion = true,
        LatestCompatibilityStrict = true,
        Validate = false, // Set this back to true for validation
    };

    public MessageConsumer(ConsumerConfig consumerConfig,
        IDeserializer<TValue>? deserializer,
        IDeadLetterMessageProducer<TKey, byte[]> dlqProducer,
        ILogger<MessageConsumer<TKey, TValue>> logger)
    {
        _consumerConfig = consumerConfig;
        _logger = logger;
        _deserializer = deserializer;
        _dlqProducer = dlqProducer;
    }
    public MessageConsumer(ConsumerConfig consumerConfig, 
        IAsyncDeserializer<TValue>? asyncDeserializer,
        IDeadLetterMessageProducer<TKey, byte[]> dlqProducer,
        ILogger<MessageConsumer<TKey, TValue>> logger)
    {
        _consumerConfig = consumerConfig;
        _logger = logger;
        _deserializer = asyncDeserializer != null ? asyncDeserializer.AsSyncOverAsync() : null;
        _dlqProducer = dlqProducer;
    }

    public MessageConsumer(ConsumerConfig consumerConfig,
        IDeadLetterMessageProducer<TKey, byte[]> dlqProducer,
        ILogger<MessageConsumer<TKey, TValue>> logger)
    {
        _consumerConfig = consumerConfig;
        _logger = logger;
        _deserializer = new CustomDeserializer<TValue>();
        _dlqProducer = dlqProducer;
    }

    public MessageConsumer(ConsumerConfig consumerConfig,
        SchemaRegistryConfig config,
        IDeadLetterMessageProducer<TKey, byte[]> dlqProducer,
        ILogger<MessageConsumer<TKey, TValue>> logger,
        JsonSerializerConfig? jsonSerializerConfig = null)
    {
        _consumerConfig = consumerConfig;
        jsonSerializerConfig ??= _jsonSerializerConfig;
        ArgumentNullException.ThrowIfNull(config, nameof(config));
        _logger = logger;

        schemaRegistryClient = new CachedSchemaRegistryClient(config);
        _deserializer = new JsonDeserializer<TValue>(schemaRegistryClient, jsonSerializerConfig).AsSyncOverAsync();
        _dlqProducer = dlqProducer;
    }
    private ActivitySourceProvider activitySource = new ActivitySourceProvider("Kafka.MessageConsumer");
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
                    var activity = activitySource.StartConsumerActivity($"topic:{topic},groupId:{groupId}");
                    var consumeResult = consumer.Consume(cancellationToken);

                    Guid correlationId = Guid.NewGuid();
                    if (consumeResult.Message.Headers.TryGetHeader(LogicalCallContext.Constants.XCorrelationId, out string strCorrelationId))
                    {
                        correlationId = Guid.Parse(strCorrelationId);
                    }
                    using var appContext = new ApplicationContextScope(correlationId);
                    //Get CorrelationId from headers and add to activity
                    using var innerScope = _logger.BeginScope(new Dictionary<string, object>
                    {
                        ["Topic"] = topic,
                        [LogicalCallContext.Constants.XCorrelationId] = ApplicationContextScope.Current.CorrelationId
                    });

                    try
                    {
                        TValue deserializedValue = DesiriliseValue(consumeResult);

                        _logger.LogInformation($"Processing Consumed message at: '{consumeResult.TopicPartitionOffset}'.");
                        var consumeStatus = await consumerFactory(consumeResult.Message.Key, deserializedValue);
                        _logger.LogInformation($"Consumed message at: '{consumeResult.TopicPartitionOffset}'.");
                        if (consumeStatus == ConsumeStatus.DeadLetter)
                        {
                            await HandleDeadLetterAsync(topic, groupId, consumeResult, new Exception("Message sent to dead letter queue due to consumer logic."), cancellationToken);
                        }
                    }
                    catch (ConsumeException e)
                    {
                        _logger.LogError($"Error consuming message: {e.Error.Reason}");
                        await HandleDeadLetterAsync(topic, groupId, consumeResult, e, cancellationToken);
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
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Unexpected error occurred.");
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

        try
        {
            return _deserializer.Deserialize(consumeResult.Message.Value, false, new SerializationContext(MessageComponentType.Value, consumeResult.Topic));
        }
        catch (Exception ex)
        {
            throw new ConsumeException(null,null, ex);
        }
    }

    private async Task HandleDeadLetterAsync(string topic, string groupId, ConsumeResult<TKey, byte[]> consumeResult, Exception exception, CancellationToken cancellationToken = default)
    {
        _logger.LogError(exception, $"Sending message to dead letter queue for topic '{topic}' and group ID '{groupId}'");
        await _dlqProducer.ProduceDeadLetterAsync(topic, groupId, consumeResult, exception, cancellationToken);
    }
}
