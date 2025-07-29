
namespace Kafka.Schemas.Shared
{
    public interface IMessageProducerBuilder<TKey, TValue> : IDisposable
        where TValue : class
    {
        Task ProduceAsync(string topic, TKey key, TValue value, IReadOnlyDictionary<string, string> headers,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}