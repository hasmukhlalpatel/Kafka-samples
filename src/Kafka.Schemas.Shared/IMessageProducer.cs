
namespace Kafka.Schemas.Shared;

public interface IMessageProducer<TKey, TValue> : IDisposable
    where TValue : class
{
    Task ProduceAsync(string topic, TKey key, TValue value, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken = default);
    Task ProduceAsync(string topic, TKey key, TValue value, CancellationToken cancellationToken = default);
}
