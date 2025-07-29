using Kafka.Schemas.Shared;
namespace KafkaProducer.WebApp.Tests
{
    public class SchemaUnitTest
    {
        [Fact]
        public async Task SendMessageTest()
        {
            // Arrange
            var topicName = "test-topic";
            using (var schemaRegistry = new SchemaRegistryService())
            {
                var id = await schemaRegistry.RegisterSchemaAsync(topicName, SchemaGenerator.GenerateSchemaJson<TestMessage>());
            }

            using (var producer = new MessageProducerBuilder<Guid, TestMessage>())
            {
                var customer = new TestCustomer
                {
                    Id = Guid.NewGuid(),
                    Name = "John Doe",
                    Age = 30,
                    Email = "test@test.com"
                };
                await producer.ProduceAsync(topicName, customer.Id, new TestMessage { Customer =customer}, null, default(CancellationToken));
                var order = new TestOrder
                {
                    Id = Guid.NewGuid(),
                    ProductName = "Sample Product",
                    Quantity = 2,
                    Price = 19.99m,
                    OrderDate = DateTime.UtcNow
                };
                await producer.ProduceAsync(topicName, customer.Id, new TestMessage { Order = order }, null, default(CancellationToken));

            }
        }

        public class TestMessage
        {
            public TestCustomer? Customer { get; set; }
            public TestOrder? Order { get; set; }
        }

        public class TestCustomer
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
            public string Email { get; set; }
        }
        public class TestOrder
        {
            public Guid Id { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public DateTime OrderDate { get; set; }
        }

    }
}
