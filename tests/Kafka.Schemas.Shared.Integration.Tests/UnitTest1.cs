namespace Kafka.Schemas.Shared.Integration.Tests;

[Collection("KafkaIntegrationTest")]
public class UnitTest1(KafkaIntegrationTestFixture testFixture)
{

    [Fact]
    public void Test1()
    {
        Assert.True(true);
    }
}
