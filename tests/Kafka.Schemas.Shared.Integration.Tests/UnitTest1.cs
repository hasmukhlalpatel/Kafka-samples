using Microsoft.Extensions.DependencyInjection;

namespace Kafka.Schemas.Shared.Integration.Tests;

[Collection("KafkaIntegrationTest")]
public class UnitTest1(KafkaIntegrationTestFixture testFixture)
{

    [Fact]
    public async Task Test1()
    {
        var producer = testFixture.Services.GetRequiredService<IMessageProducer<string, TestMessage>>();
        await producer.ProduceAsync(testFixture.TopicName, "key1", new TestMessage { Id = 1, Name = "Test" });
        Assert.True(true);
    }
}
