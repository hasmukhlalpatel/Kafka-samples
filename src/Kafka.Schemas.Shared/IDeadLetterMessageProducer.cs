
using Confluent.Kafka;

namespace Kafka.Schemas.Shared;

public interface IDeadLetterMessageProducer<TKey, TValue> : IDisposable
    where TValue : class
{
    Task ProduceDeadLetterAsync(string originalTopic, string consumerGroup, ConsumeResult<TKey, TValue> consumeResult, Exception exception, CancellationToken cancellationToken = default);
}