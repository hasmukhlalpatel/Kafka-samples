using Confluent.Kafka;
using Kafka.Schemas.Shared;
namespace KafkaProducer.WebApp.Tests
{
    public class SchemaUnitTest
    {
        [Fact]
        public async Task SendMessageTest()
        {
            // Arrange
            var topicName = "test-topic-2";
            using (var schemaRegistry = new SchemaRegistryService())
            {
                var id = await schemaRegistry.RegisterSchemaAsync(topicName, SchemaGenerator.GenerateSchemaJson<TestMessage>());
            }

            using (var producer = new MessageProducerBuilder<Confluent.Kafka.Null, TestMessage>())
            {
                var customer = new TestCustomer
                {
                    Id = Guid.NewGuid(),
                    Name = "John Doe",
                    Age = 30,
                    Email = "test@test.com"
                };
                var customerEvent = new CustomerCreateEvent { Customer = customer };
                var customerMessage = new TestMessage { CustomerEventData = customerEvent };
                var message = new Message<Confluent.Kafka.Null, TestMessage>
                {
                    Value = customerMessage
                };
                // Act
                await producer.ProduceAsync(topicName, message);
                //await producer.ProduceAsync(topicName, customer.Id, new TestMessage { Customer =customer}, null, default(CancellationToken));
                var order = new TestOrder
                {
                    Id = Guid.NewGuid(),
                    ProductName = "Sample Product",
                    Quantity = 2,
                    Price = 19.99m,
                    OrderDate = DateTime.UtcNow
                };
                var orderEvent = new OrderCreateEvent { Customer = customer, Order = order };
                var orderMessage = new TestMessage { OrderEventData = orderEvent };
                message = new Message<Confluent.Kafka.Null, TestMessage>
                {
                    Value = orderMessage
                };
                await producer.ProduceAsync(topicName, message);
                //await producer.ProduceAsync(topicName,  new TestMessage { Order = order }, null, default(CancellationToken));

            }
        }

        public class TestMessage
        {
            public CustomerCreateEvent? CustomerEventData { get; set; }
            public OrderCreateEvent? OrderEventData { get; set; }
        }

        public class CustomerCreateEvent
        {
            public TestCustomer Customer { get; set; }
        }

        public class OrderCreateEvent
        {
            public TestCustomer Customer { get; set; }
            public TestOrder Order { get; set; }
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
