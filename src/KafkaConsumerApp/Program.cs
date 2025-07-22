
using com.example.schemas;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Kafka.Schemas.Shared;
using Newtonsoft.Json; // For JsonConvert
using Newtonsoft.Json.Linq;
using System.Text; // For JObject

namespace KafkaConsumerApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string bootstrapServers = KafkaConfig.Default.BootstrapServers;
            string schemaRegistryUrl = KafkaConfig.Default.SchemaRegistryUrl;
            string topicName = KafkaConfig.Default.TopicName;
            string groupId = KafkaConfig.Default.ConsumerGroupId;
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

                            // First, try to deserialize to the base OrderMessage to get the OrderType
                            var orderType = receivedJObject["OrderType"].ToString();
                            // --- Read Header Information ---
                            string eventName = "N/A";
                            if (consumeResult.Message.Headers != null)
                            {
                                var eventNameHeader = consumeResult.Message.Headers.FirstOrDefault(h => h.Key == "eventname");
                                if (eventNameHeader != null)
                                {
                                    eventName = Encoding.UTF8.GetString(eventNameHeader.GetValueBytes());
                                }
                            }
                            Console.WriteLine($"  Header 'eventname': {eventName}");
                            // --- End Read Header Information ---

                            // Now, use the OrderType property to determine the specific message type
                            if (orderType == "StandardOrder")
                            {
                                var standardOrder = receivedJObject.ToObject<StandardOrder>();
                                Console.WriteLine($"  Type: Standard Order");
                                Console.WriteLine($"  Customer: {standardOrder.CustomerInfo.Name}, Product: {standardOrder.ProductInfo.Name} (Features: {standardOrder.ProductInfo.StandardProductFeatures})");
                                Console.WriteLine($"  Standard Features: {standardOrder.StandardFeatures}");
                            }
                            else if (orderType == "PremiumOrder")
                            {
                                var premiumOrder = receivedJObject.ToObject<PremiumOrder>();
                                Console.WriteLine($"  Type: Premium Order");
                                Console.WriteLine($"  Customer: {premiumOrder.CustomerInfo.Name}, Product: {premiumOrder.ProductInfo.Name} (Features: {premiumOrder.ProductInfo.PremiumProductFeatures})");
                                Console.WriteLine($"  Premium Discount: {premiumOrder.PremiumDiscountPercentage}%, Support: {premiumOrder.DedicatedSupportContact}");
                            }
                            else
                            {
                                Console.WriteLine($"  Type: Unknown Order Message (OrderType: {orderType})");
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
