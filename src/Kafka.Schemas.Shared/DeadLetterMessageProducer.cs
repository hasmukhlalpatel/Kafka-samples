using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Kafka.Schemas.Shared;

public class DeadLetterMessageProducer<TKey, TValue> : MessageProducer<TKey, TValue>, IDeadLetterMessageProducer<TKey, TValue>
    where TValue : class
{
    public DeadLetterMessageProducer(ProducerConfig producerConfig, ILogger<MessageProducer<TKey, TValue>> logger) : base(producerConfig, logger)
    {
    }

    public async Task ProduceDeadLetterAsync(string originalTopic, string consumerGroup, TKey key, TValue value, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default)
    {
        var deadLetterTopic = $"{originalTopic}-{consumerGroup}-dead-letter";
        await ProduceAsync(deadLetterTopic, key, value, headers, cancellationToken);
    }
}