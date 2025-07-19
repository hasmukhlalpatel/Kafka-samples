using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Newtonsoft.Json.Schema.Generation; // Required for JSchemaGenerator

namespace KafkaProducerApp
{
    // Define the C# classes that will be serialized to JSON
    // These are now simple POCOs, no Avro-specific attributes or interfaces
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
            // Using DateTimeOffset for better handling of timestamps in .NET
            public DateTimeOffset Timestamp { get; set; }
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
            string topicName = "my-dotnet-json-topic"; // New topic for JSON Schema messages

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
            {
                try
                {
                    // --- Explicit JSON Schema Generation and Registration at Startup ---

                    // Create a JSchemaGenerator instance
                    var generator = new JSchemaGenerator();
                    // Generate the JSON Schema from the C# OrderMessage class
                    Newtonsoft.Json.Schema.JSchema jsonSchema = generator.Generate(typeof(com.example.schemas.OrderMessage));

                    // Convert the JSchema object to its JSON string representation
                    string jsonSchemaString = jsonSchema.ToString();

                    // Define the subject name for the schema. For value schemas, it's typically "<topic-name>-value"
                    string subject = $"{topicName}-value";

                    Console.WriteLine($"Attempting to register JSON schema for subject: {subject}");
                    Console.WriteLine($"Generated JSON Schema:\n{jsonSchemaString}");

                    // Register the schema with the Schema Registry
                    // Specify SchemaType.Json for JSON Schema
                    var schema = new Confluent.SchemaRegistry.Schema(jsonSchemaString, SchemaType.Json);
                    int schemaId = await schemaRegistry.RegisterSchemaAsync(subject, schema);
                    Console.WriteLine($"JSON Schema registered successfully with ID: {schemaId}");

                    // --- End Explicit JSON Schema Registration ---

                    // Build the producer with JsonSerializer for the OrderMessage type
                    using (var producer = new ProducerBuilder<Null, com.example.schemas.OrderMessage>(producerConfig)
                        .SetValueSerializer(new JsonSerializer<com.example.schemas.OrderMessage>(schemaRegistry))
                        .Build())
                    {
                        for (int i = 0; i < 3; i++) // Send 3 messages
                        {
                            var orderMessage = new com.example.schemas.OrderMessage
                            {
                                CustomerInfo = new com.example.schemas.Customer { Id = 101 + i, Name = $"Customer {i + 1}" },
                                ProductInfo = new com.example.schemas.Product { Id = 201 + i, Name = $"Product {i + 1}X" },
                                ContactInfo = new com.example.schemas.Contact { Id = 301 + i, Name = $"Contact {i + 1}Y" },
                                Timestamp = DateTimeOffset.UtcNow // Using DateTimeOffset directly
                            };

                            var deliveryResult = await producer.ProduceAsync(topicName, new Message<Null, com.example.schemas.OrderMessage> { Value = orderMessage });

                            Console.WriteLine($"Delivered message to topic: {deliveryResult.Topic}, partition: {deliveryResult.Partition}, offset: {deliveryResult.Offset}");
                            Console.WriteLine($"  Customer: {orderMessage.CustomerInfo.Name}, Product: {orderMessage.ProductInfo.Name}, Contact: {orderMessage.ContactInfo.Name}");
                        }
                    }
                }
                catch (ProduceException<Null, com.example.schemas.OrderMessage> e)
                {
                    Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                }
                catch (Confluent.SchemaRegistry.SchemaRegistryException e)
                {
                    Console.WriteLine($"Schema Registry error: {e.Message}");
                    Console.WriteLine($"Error Code: {e.ErrorCode}");
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
