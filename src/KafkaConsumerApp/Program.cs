
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Newtonsoft.Json; // For JsonConvert
using Newtonsoft.Json.Linq; // For JObject

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
        public class StandardProduct : Product
        {
            public string StandardProductFeatures { get; set; }
        }
        public class PremiumProduct : Product
        {
            public string PremiumProductFeatures { get; set; }
        }

        public class Contact
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        // Base OrderMessage class
        public class OrderMessage
        {
            public Customer CustomerInfo { get; set; }
            public Contact ContactInfo { get; set; }
            public DateTimeOffset Timestamp { get; set; }
        }

        // Specific message types for the union, inheriting from OrderMessage
        public class StandardOrderMessage : OrderMessage
        {
            public StandardProduct ProductInfo { get; set; }
            public string StandardFeatures { get; set; }
        }

        public class PremiumOrderMessage : OrderMessage
        {
            public PremiumProduct ProductInfo { get; set; }
            public int PremiumDiscountPercentage { get; set; }
            public string DedicatedSupportContact { get; set; }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            string bootstrapServers = "localhost:9092";
            string schemaRegistryUrl = "http://localhost:8081";
            string topicName = "my-dotnet-union-json-topic"; // Topic for union schema
            string groupId = "my-dotnet-union-json-consumer-group";

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            var schemaRegistryConfig = new SchemaRegistryConfig { Url = schemaRegistryUrl };

            Console.WriteLine($"Starting Kafka Consumer for topic: {topicName}, group: {groupId}");
            Console.WriteLine($"Connecting to Schema Registry at: {schemaRegistryUrl}");

            CancellationTokenSource cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true;
                cts.Cancel();
            };

            using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
            // Build the consumer with JsonDeserializer for JObject
            // We receive as JObject because the actual type is dynamic (Standard or Premium)
            using (var consumer = new ConsumerBuilder<Ignore, JObject>(consumerConfig)
                .SetValueDeserializer(new JsonDeserializer<JObject>(schemaRegistry).AsSyncOverAsync())
                .Build())
            {
                consumer.Subscribe(topicName);

                try
                {
                    while (!cts.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(cts.Token);
                            JObject receivedJObject = consumeResult.Message.Value;

                            Console.WriteLine($"Consumed message from topic: {consumeResult.Topic}, partition: {consumeResult.Partition}, offset: {consumeResult.Offset}");

                            // Dynamically determine the message type based on the presence of specific fields
                            // and then deserialize into the correct concrete type.
                            if (receivedJObject.ContainsKey("standardFeatures"))
                            {
                                var standardOrder = receivedJObject.ToObject<com.example.schemas.StandardOrderMessage>();
                                Console.WriteLine($"  Type: Standard Order");
                                Console.WriteLine($"  Customer: {standardOrder.CustomerInfo.Name}, Product: {standardOrder.ProductInfo.Name} (Features: {standardOrder.ProductInfo.StandardProductFeatures})");
                                Console.WriteLine($"  Standard Features: {standardOrder.StandardFeatures}");
                            }
                            else if (receivedJObject.ContainsKey("premiumDiscountPercentage") && receivedJObject.ContainsKey("dedicatedSupportContact"))
                            {
                                var premiumOrder = receivedJObject.ToObject<com.example.schemas.PremiumOrderMessage>();
                                Console.WriteLine($"  Type: Premium Order");
                                Console.WriteLine($"  Customer: {premiumOrder.CustomerInfo.Name}, Product: {premiumOrder.ProductInfo.Name} (Features: {premiumOrder.ProductInfo.PremiumProductFeatures})");
                                Console.WriteLine($"  Premium Discount: {premiumOrder.PremiumDiscountPercentage}%, Support: {premiumOrder.DedicatedSupportContact}");
                            }
                            else
                            {
                                Console.WriteLine($"  Type: Unknown Order Message");
                                Console.WriteLine($"  Raw JSON: {receivedJObject.ToString(Formatting.Indented)}");
                            }
                        }
                        catch (ConsumeException e)
                        {
                            Console.WriteLine($"Error consuming message: {e.Error.Reason}");
                        }
                        catch (OperationCanceledException)
                        {
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
                    consumer.Close();
                }
            }

            Console.WriteLine("Consumer stopped. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
