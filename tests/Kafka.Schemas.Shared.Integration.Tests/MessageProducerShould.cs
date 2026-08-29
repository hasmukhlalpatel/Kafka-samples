using Microsoft.Extensions.DependencyInjection;
using Observability.Shared;

namespace Kafka.Schemas.Shared.Integration.Tests;

[Collection("KafkaIntegrationTest")]
public class MessageProducerShould(KafkaIntegrationTestFixture testFixture)
{

    [Fact]
    public async Task DeliverMessageToTopic()
    {
        using var scope = new ApplicationContextScope();
        var producer = testFixture.Services.GetRequiredService<IMessageProducer<string, TestMessage>>();
        await producer.ProduceAsync(testFixture.TopicName, "key1", new TestMessage { Id = 1, Name = "Test" });
        var cancelationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(50));

        var consumer = testFixture.Services.GetRequiredService<IMessageConsumer<string, TestMessage>>();
        await consumer.StartConsumingAsync(testFixture.TopicName, async (key, value) =>
        {
            await Task.Delay(100); // Simulate processing time
            cancelationTokenSource.Cancel();
            return ConsumeStatus.Success;
        }, cancelationTokenSource.Token);
        Assert.True(true);
    }
}
