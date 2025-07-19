using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;

namespace KafkaConsumerApp
{
    // Re-using the C# class definitions for deserialization
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
            public DateTimeOffset Timestamp { get; set; } // Using DateTimeOffset directly
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            // Kafka broker address
            string bootstrapServers = "localhost:9092"; // Change if your Kafka is elsewhere
            // Schema Registry address
            string schemaRegistryUrl = "http://localhost:8081"; // Change if your Schema Registry is elsewhere
            string topicName = "my-dotnet-json-topic"; // Topic for JSON Schema messages
            string groupId = "my-dotnet-json-consumer-group"; // Consumer group ID

            // Consumer configuration
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest, // Start consuming from the beginning if no offset is stored
                EnableAutoCommit = true // Automatically commit offsets
            };

            // Schema Registry configuration
            var schemaRegistryConfig = new SchemaRegistryConfig
            {
                Url = schemaRegistryUrl
            };

            Console.WriteLine($"Starting Kafka Consumer for topic: {topicName}, group: {groupId}");
            Console.WriteLine($"Connecting to Schema Registry at: {schemaRegistryUrl}");

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true; // Prevent the process from terminating immediately
                cts.Cancel();
            };

            // Create a Schema Registry client
            using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
            // Build the consumer with JsonDeserializer for the OrderMessage type
            using (var consumer = new ConsumerBuilder<Ignore, com.example.schemas.OrderMessage>(consumerConfig)
                .SetValueDeserializer(new JsonDeserializer<com.example.schemas.OrderMessage>(schemaRegistry).AsSyncOverAsync())
                .Build())
            {
                consumer.Subscribe(topicName);

                try
                {
                    while (!cts.IsCancellationRequested)
                    {
                        try
                        {
                            // Consume a message with a timeout
                            var consumeResult = consumer.Consume(cts.Token);

                            // The JsonDeserializer handles deserialization into the OrderMessage object
                            com.example.schemas.OrderMessage receivedMessage = consumeResult.Message.Value;

                            Console.WriteLine($"Consumed message from topic: {consumeResult.Topic}, partition: {consumeResult.Partition}, offset: {consumeResult.Offset}");
                            Console.WriteLine($"  Customer: Id={receivedMessage.CustomerInfo.Id}, Name='{receivedMessage.CustomerInfo.Name}'");
                            Console.WriteLine($"  Product: Id={receivedMessage.ProductInfo.Id}, Name='{receivedMessage.ProductInfo.Name}'");
                            Console.WriteLine($"  Contact: Id={receivedMessage.ContactInfo.Id}, Name='{receivedMessage.ContactInfo.Name}'");
                            Console.WriteLine($"  Timestamp: {receivedMessage.Timestamp.LocalDateTime}");
                        }
                        catch (ConsumeException e)
                        {
                            Console.WriteLine($"Error consuming message: {e.Error.Reason}");
                        }
                        catch (OperationCanceledException)
                        {
                            // User pressed Ctrl+C
                            Console.WriteLine("Consumer shutting down...");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"An unexpected error occurred during consumption: {e.Message}");
                        }
                    }
                }
                finally
                {
                    consumer.Close(); // Commit offsets and leave the group cleanly
                }
            }

            Console.WriteLine("Consumer stopped. Press any key to exit.");
            Console.ReadKey();
        }
    }
}