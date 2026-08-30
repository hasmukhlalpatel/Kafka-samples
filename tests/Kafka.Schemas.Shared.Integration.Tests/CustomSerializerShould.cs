using Confluent.Kafka;
using Kafka.Schemas.Shared.Serialization;

namespace Kafka.Schemas.Shared.Integration.Tests
{
    public class CustomSerializerShould
    {
        [Fact]
        public async Task SerializeAndDeserializeMessage()
        {
            // Arrange
            var serializer = new CustomSerializer<TestMessage>(defaultSchemaId: 1);
            var deserializer = new CustomDeserializer<TestMessage>();
            var originalMessage = new TestMessage { Id = 42, Name = "Test Message" };
            // Act
            var serializedData = await serializer.SerializeAsync(originalMessage, new SerializationContext(MessageComponentType.Value, "test-topic"));
            var deserializedMessage = deserializer.Deserialize(serializedData, false, new SerializationContext(MessageComponentType.Value, "test-topic"));
            // Assert
            Assert.NotNull(deserializedMessage);
            Assert.Equal(originalMessage.Id, deserializedMessage.Id);
            Assert.Equal(originalMessage.Name, deserializedMessage.Name);
        }
    }
}
