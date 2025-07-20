// KafkaProducerApp/Program.cs
using com.example.schemas;
using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Kafka.Schemas.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using GenerateSchema = KafkaProducerApp.SchemaGenerator;

namespace KafkaProducerApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Use settings from shared KafkaConfig
            string bootstrapServers = KafkaConfig.Default.BootstrapServers;
            string schemaRegistryUrl = KafkaConfig.Default.SchemaRegistryUrl;
            string topicName = KafkaConfig.Default.TopicName;

            var producerConfig = new ProducerConfig { BootstrapServers = bootstrapServers };
            var schemaRegistryConfig = new SchemaRegistryConfig { Url = schemaRegistryUrl };

            Console.WriteLine($"Starting Kafka Producer for topic: {topicName}");
            Console.WriteLine($"Connecting to Schema Registry at: {schemaRegistryUrl}");

            using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
            {
                try
                {
                    // --- Explicit JSON Schema Generation and Registration at Startup ---

                    // Generate schemas for all relevant types
                    // Using the new static GenerateSchema<T>() helper method
                    var customerSchemaString = GenerateSchema.GenerateSchema<Customer>();
                    var productSchemaString = GenerateSchema.GenerateSchema<Product>();
                    var standardProductSchemaString = GenerateSchema.GenerateSchema<StandardProduct>();
                    var premiumProductSchemaString = GenerateSchema.GenerateSchema<PremiumProduct>();
                    var contactSchemaString = GenerateSchema.GenerateSchema<Contact>();
                    var orderMessageBaseSchemaString = GenerateSchema.GenerateSchema<OrderMessage>();
                    var standardOrderSchemaString =  GenerateSchema.GenerateSchema<StandardOrderMessage>();
                    var premiumOrderSchemaString = GenerateSchema.GenerateSchema<PremiumOrderMessage>();


                    // Manually construct the root JSON Schema with 'anyOf'
                    // This schema defines that the message can be EITHER StandardOrderMessage OR PremiumOrderMessage
                    // We embed all relevant schemas as definitions and reference them.
                    string rootSchemaJsonString = $@"
                    {{
                        ""title"": ""OrderEvent"",
                        ""anyOf"": [
                            {{ ""$ref"": ""#/definitions/StandardOrderMessage"" }},
                            {{ ""$ref"": ""#/definitions/PremiumOrderMessage"" }}
                        ],
                        ""definitions"": {{
                            ""Customer"": {customerSchemaString},
                            ""Product"": {productSchemaString},
                            ""StandardProduct"": {standardProductSchemaString},
                            ""PremiumProduct"": {premiumProductSchemaString},
                            ""Contact"": {contactSchemaString},
                            ""OrderMessage"": {orderMessageBaseSchemaString},
                            ""StandardOrderMessage"": {standardOrderSchemaString},
                            ""PremiumOrderMessage"": {premiumOrderSchemaString}
                        }}
                    }}";

                    string subject = $"{topicName}-value";

                    Console.WriteLine($"Attempting to register JSON schema for subject: {subject}");
                    Console.WriteLine($"Generated JSON Schema:\n{JToken.Parse(rootSchemaJsonString).ToString(Formatting.Indented)}");

                    var schema = new Confluent.SchemaRegistry.Schema(rootSchemaJsonString, SchemaType.Json);
                    int schemaId = await schemaRegistry.RegisterSchemaAsync(subject, schema);
                    Console.WriteLine($"JSON Schema registered successfully with ID: {schemaId}");

                    // --- End Explicit JSON Schema Registration ---

                    using (var producer = new ProducerBuilder<Null, JObject>(producerConfig)
                        .SetValueSerializer(new JsonSerializer<JObject>(schemaRegistry))
                        .Build())
                    {
                        // Send a StandardOrderMessage
                        var standardOrder = new StandardOrderMessage
                        {
                            CustomerInfo = new Customer { Id = 1, Name = "Alice Smith" },
                            ProductInfo = new StandardProduct { Id = 101, Name = "Basic Service", StandardProductFeatures = "Feature A" },
                            ContactInfo = new Contact { Id = 201, Name = "Support A" },
                            Timestamp = DateTimeOffset.UtcNow,
                            StandardFeatures = "Email notifications"
                        };
                        JObject standardOrderJObject = JObject.FromObject(standardOrder);

                        // Create Headers and add eventname
                        Headers standardHeaders = new Headers();
                        standardHeaders.Add("eventname", Encoding.UTF8.GetBytes("StandardOrderCreated")); // Add eventname header

                        await producer.ProduceAsync(topicName, new Message<Null, JObject> { Value = standardOrderJObject, Headers = standardHeaders });
                        Console.WriteLine($"Delivered Standard Order: {standardOrder.CustomerInfo.Name} with eventname: StandardOrderCreated");

                        // Send a PremiumOrderMessage
                        var premiumOrder = new PremiumOrderMessage
                        {
                            CustomerInfo = new Customer { Id = 2, Name = "Bob Johnson" },
                            ProductInfo = new PremiumProduct { Id = 102, Name = "Premium Package", PremiumProductFeatures = "Feature X" },
                            ContactInfo = new Contact { Id = 202, Name = "Dedicated Manager B" },
                            Timestamp = DateTimeOffset.UtcNow.AddMinutes(1),
                            PremiumDiscountPercentage = 15,
                            DedicatedSupportContact = "John Doe"
                        };
                        JObject premiumOrderJObject = JObject.FromObject(premiumOrder);

                        // Create Headers and add eventname
                        Headers premiumHeaders = new Headers();
                        premiumHeaders.Add("eventname", Encoding.UTF8.GetBytes("PremiumOrderCreated")); // Add eventname header

                        await producer.ProduceAsync(topicName, new Message<Null, JObject> { Value = premiumOrderJObject, Headers = premiumHeaders });
                        Console.WriteLine($"Delivered Premium Order: {premiumOrder.CustomerInfo.Name} with eventname: PremiumOrderCreated");

                        // Send another StandardOrderMessage
                        var standardOrder2 = new StandardOrderMessage
                        {
                            CustomerInfo = new Customer { Id = 3, Name = "Charlie Brown" },
                            ProductInfo = new StandardProduct { Id = 103, Name = "Basic Service Plus", StandardProductFeatures = "Feature B" },
                            ContactInfo = new Contact { Id = 203, Name = "Support C" },
                            Timestamp = DateTimeOffset.UtcNow.AddMinutes(2),
                            StandardFeatures = "SMS alerts"
                        };
                        JObject standardOrder2JObject = JObject.FromObject(standardOrder2);

                        // Create Headers and add eventname
                        Headers standardHeaders2 = new Headers();
                        standardHeaders2.Add("eventname", Encoding.UTF8.GetBytes("StandardOrderCreated")); // Add eventname header

                        await producer.ProduceAsync(topicName, new Message<Null, JObject> { Value = standardOrder2JObject, Headers = standardHeaders2 });
                        Console.WriteLine($"Delivered Standard Order: {standardOrder2.CustomerInfo.Name} with eventname: StandardOrderCreated");
                    }
                }
                catch (ProduceException<Null, JObject> e)
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
