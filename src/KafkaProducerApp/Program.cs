
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace KafkaProducerApp
{
    // Define the C# classes that correspond to your Avro schemas
    // These will be serialized/deserialized by the Avro serializer/deserializer
    namespace com.example.schemas
    {
        public class Customer
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class Contact
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class OrderMessage
        {
            public Customer CustomerInfo { get; set; }
            public Product ProductInfo { get; set; }
            public Contact ContactInfo { get; set; }
            // Avro's 'timestamp-millis' logical type maps to long (milliseconds since Unix epoch) in C#
            public long Timestamp { get; set; }
        }
    }

    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Kafka broker address
            string bootstrapServers = "localhost:9092"; // Change if your Kafka is elsewhere
            // Schema Registry address
            string schemaRegistryUrl = "http://localhost:8081"; // Change if your Schema Registry is elsewhere
            string topicName = "my-dotnet-avro-topic"; // New topic for Avro messages

            // Producer configuration
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = bootstrapServers
            };

            // Schema Registry configuration
            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = schemaRegistryUrl
            };

            Console.WriteLine($"Starting Kafka Producer for topic: {topicName}");
            Console.WriteLine($"Connecting to Schema Registry at: {schemaRegistryUrl}");

            // Create a Schema Registry client
            using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
            // Build the producer with AvroSerializer for the OrderMessage type
            using (var producer = new ProducerBuilder<Null, com.example.schemas.OrderMessage>(producerConfig)
                .SetValueSerializer(new AvroSerializer<com.example.schemas.OrderMessage>(schemaRegistry))
                .Build())
            {
                try
                {
                    for (int i = 0; i < 3; i++) // Send 3 messages
                    {
                        var orderMessage = new com.example.schemas.OrderMessage
                        {
                            CustomerInfo = new com.example.schemas.Customer { Id = 101 + i, Name = $"Customer {i + 1}" },
                            ProductInfo = new com.example.schemas.Product { Id = 201 + i, Name = $"Product {i + 1}X" },
                            ContactInfo = new com.example.schemas.Contact { Id = 301 + i, Name = $"Contact {i + 1}Y" },
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() // Convert DateTime to milliseconds since epoch
                        };

                        var deliveryResult = await producer.ProduceAsync(topicName, new Message<Null, com.example.schemas.OrderMessage> { Value = orderMessage });

                        Console.WriteLine($"Delivered message to topic: {deliveryResult.Topic}, partition: {deliveryResult.Partition}, offset: {deliveryResult.Offset}");
                        Console.WriteLine($"  Customer: {orderMessage.CustomerInfo.Name}, Product: {orderMessage.ProductInfo.Name}, Contact: {orderMessage.ContactInfo.Name}");
                    }
                }
                catch (ProduceException<Null, com.example.schemas.OrderMessage> e)
                {
                    Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"An unexpected error occurred: {e.Message}");
                }
            }

            Console.WriteLine("Producer finished sending messages. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
