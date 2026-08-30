using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Kafka.Schemas.Shared;

public class DeadLetterMessageProducer<TKey, TValue> : MessageProducer<TKey, TValue>,
    IDeadLetterMessageProducer<TKey, TValue>
    where TValue : class
{
    public DeadLetterMessageProducer(ProducerConfig producerConfig, ILogger<DeadLetterMessageProducer<TKey, TValue>> logger) : base(producerConfig, logger)
    {
    }

    public async Task ProduceDeadLetterAsync(string originalTopic, string consumerGroup, ConsumeResult<TKey, TValue> consumeResult, Exception exception, CancellationToken cancellationToken = default)
    {
        var deadLetterTopic = $"{originalTopic}-{consumerGroup}-dead-letter";
        await ProduceAsync(deadLetterTopic, consumeResult.Message.Key, consumeResult.Message.Value, null, cancellationToken);
    }
}