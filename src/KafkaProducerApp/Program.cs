using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Newtonsoft.Json.Linq;
using NJsonSchema.NewtonsoftJson.Generation; // For JObject

namespace KafkaProducerApp
{
    // Common nested types
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
            public StandardProduct ProductInfo { get; set; } // Now StandardProduct
            public string StandardFeatures { get; set; } // Specific field for standard orders
        }

        public class PremiumOrderMessage : OrderMessage
        {
            public PremiumProduct ProductInfo { get; set; } // Now PremiumProduct
            public int PremiumDiscountPercentage { get; set; } // Specific field for premium orders
            public string DedicatedSupportContact { get; set; } // Another specific field
        }
    }

    public class Program
    {
        // Helper method to generate JSON Schema from a C# type
        public static string GenerateSchema<T>() => NewtonsoftJsonSchemaGenerator.FromType<T>(new NewtonsoftJsonSchemaGeneratorSettings
        {
            SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                Formatting = Newtonsoft.Json.Formatting.Indented,
                //Converters = [new Samples.Serialization.Library.PolymorphicJsonConverter<T>()]

            }
        }).ToJson();

        public static async Task Main(string[] args)
        {
            string bootstrapServers = "localhost:9092";
            string schemaRegistryUrl = "http://localhost:8081";
            string topicName = "my-dotnet-union-json-topic"; // New topic for union schema

            var producerConfig = new ProducerConfig { BootstrapServers = bootstrapServers };
            var schemaRegistryConfig = new SchemaRegistryConfig { Url = schemaRegistryUrl };

            Console.WriteLine($"Starting Kafka Producer for topic: {topicName}");
            Console.WriteLine($"Connecting to Schema Registry at: {schemaRegistryUrl}");

            using (var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig))
            {
                try
                {
                    // --- Explicit JSON Schema Generation and Registration at Startup ---

                    // Generate schemas for all relevant types, including the new product types
                    // Using the new static GenerateSchema<T>() helper method
                    var customerSchemaString = GenerateSchema<com.example.schemas.Customer>();
                    var productSchemaString = GenerateSchema<com.example.schemas.Product>(); // Base Product
                    var standardProductSchemaString = GenerateSchema<com.example.schemas.StandardProduct>();
                    var premiumProductSchemaString = GenerateSchema<com.example.schemas.PremiumProduct>();
                    var contactSchemaString = GenerateSchema<com.example.schemas.Contact>();
                    var orderMessageBaseSchemaString = GenerateSchema<com.example.schemas.OrderMessage>(); // Base OrderMessage
                    var standardOrderSchemaString = GenerateSchema<com.example.schemas.StandardOrderMessage>();
                    var premiumOrderSchemaString = GenerateSchema<com.example.schemas.PremiumOrderMessage>();


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

                    // Define the subject name for the schema
                    string subject = $"{topicName}-value";

                    Console.WriteLine($"Attempting to register JSON schema for subject: {subject}");
                    Console.WriteLine($"Generated JSON Schema:\n{JToken.Parse(rootSchemaJsonString).ToString(Newtonsoft.Json.Formatting.Indented)}"); // Pretty print

                    // Register the schema with the Schema Registry
                    var schema = new Confluent.SchemaRegistry.Schema(rootSchemaJsonString, SchemaType.Json);
                    int schemaId = await schemaRegistry.RegisterSchemaAsync(subject, schema);
                    Console.WriteLine($"JSON Schema registered successfully with ID: {schemaId}");

                    // --- End Explicit JSON Schema Registration ---

                    // Build the producer with JsonSerializer for JObject
                    // We use JObject because the actual type sent will vary (Standard or Premium)
                    using (var producer = new ProducerBuilder<Null, JObject>(producerConfig)
                        .SetValueSerializer(new JsonSerializer<JObject>(schemaRegistry))
                        .Build())
                    {
                        // Send a StandardOrderMessage
                        var standardOrder = new com.example.schemas.StandardOrderMessage
                        {
                            CustomerInfo = new com.example.schemas.Customer { Id = 1, Name = "Alice Smith" },
                            ProductInfo = new com.example.schemas.StandardProduct { Id = 101, Name = "Basic Service", StandardProductFeatures = "Feature A" },
                            ContactInfo = new com.example.schemas.Contact { Id = 201, Name = "Support A" },
                            Timestamp = DateTimeOffset.UtcNow,
                            StandardFeatures = "Email notifications"
                        };
                        JObject standardOrderJObject = JObject.FromObject(standardOrder);
                        await producer.ProduceAsync(topicName, new Message<Null, JObject> { Value = standardOrderJObject });
                        Console.WriteLine($"Delivered Standard Order: {standardOrder.CustomerInfo.Name}");

                        // Send a PremiumOrderMessage
                        var premiumOrder = new com.example.schemas.PremiumOrderMessage
                        {
                            CustomerInfo = new com.example.schemas.Customer { Id = 2, Name = "Bob Johnson" },
                            ProductInfo = new com.example.schemas.PremiumProduct { Id = 102, Name = "Premium Package", PremiumProductFeatures = "Feature X" },
                            ContactInfo = new com.example.schemas.Contact { Id = 202, Name = "Dedicated Manager B" },
                            Timestamp = DateTimeOffset.UtcNow.AddMinutes(1),
                            PremiumDiscountPercentage = 15,
                            DedicatedSupportContact = "John Doe"
                        };
                        JObject premiumOrderJObject = JObject.FromObject(premiumOrder);
                        await producer.ProduceAsync(topicName, new Message<Null, JObject> { Value = premiumOrderJObject });
                        Console.WriteLine($"Delivered Premium Order: {premiumOrder.CustomerInfo.Name}");

                        // Send another StandardOrderMessage
                        var standardOrder2 = new com.example.schemas.StandardOrderMessage
                        {
                            CustomerInfo = new com.example.schemas.Customer { Id = 3, Name = "Charlie Brown" },
                            ProductInfo = new com.example.schemas.StandardProduct { Id = 103, Name = "Basic Service Plus", StandardProductFeatures = "Feature B" },
                            ContactInfo = new com.example.schemas.Contact { Id = 203, Name = "Support C" },
                            Timestamp = DateTimeOffset.UtcNow.AddMinutes(2),
                            StandardFeatures = "SMS alerts"
                        };
                        JObject standardOrder2JObject = JObject.FromObject(standardOrder2);
                        await producer.ProduceAsync(topicName, new Message<Null, JObject> { Value = standardOrder2JObject });
                        Console.WriteLine($"Delivered Standard Order: {standardOrder2.CustomerInfo.Name}");
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
