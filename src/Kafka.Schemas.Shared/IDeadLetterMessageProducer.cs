
namespace Kafka.Schemas.Shared;

public interface IDeadLetterMessageProducer<TKey, TValue> : IDisposable
    where TValue : class
{
    Task ProduceDeadLetterAsync(string originalTopic, string consumerGroup, TKey key, TValue value, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default);
}

public interface ICommonDLQProducer<TKey> : IDeadLetterMessageProducer<TKey, byte[]>
{

}