
namespace Kafka.Schemas.Shared;

public interface IMessageConsumer<TKey, TValue> 
    where TValue : class
{
    Task StartConsumingAsync(string topic, Func<TKey, TValue, Task<ConsumeStatus>> consumerFactory, CancellationToken cancellationToken = default);
    Task StartConsumingAsync(string topic, string groupId, Func<TKey, TValue, Task<ConsumeStatus>> consumerFactory, CancellationToken cancellationToken = default);
}