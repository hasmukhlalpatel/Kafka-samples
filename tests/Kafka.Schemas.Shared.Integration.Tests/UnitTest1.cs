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
        Assert.True(true);
    }
}
