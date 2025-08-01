using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using NJsonSchema;
using Newtonsoft.Json;
using System.Text;
using Xunit;

namespace KafkaProducer.WebApp.Tests;

public class KafkaSchemaRegistryTest : IAsyncLifetime
{
    private const string KafkaContainerAlias = "kafka";
    private const string SchemaRegistryAlias = "schema-registry";
    private const int KafkaPort = 9092;
    private const int SchemaRegistryPort = 8081;

    private readonly INetwork _network;
    private readonly IContainer _kafkaContainer;
    private readonly IContainer _schemaRegistryContainer;

    private const string Topic = "orders";
    private const string zookeeperImage = "confluentinc/cp-zookeeper:7.6.0"; // Use a specific version
    private const string KafkaImage = "confluentinc/cp-kafka:7.6.0"; // Use a specific version
    private const string SchemaRegistryImage = "confluentinc/cp-schema-registry:7.6.0"; // Use a specific version

    public KafkaSchemaRegistryTest()
    {
        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        _kafkaContainer = new ContainerBuilder()
            .WithName("kafka-test")
            .WithImage(KafkaImage)
            .WithPortBinding(KafkaPort, true)
            .WithEnvironment("KAFKA_BROKER_ID", "1")
            .WithEnvironment("KAFKA_ZOOKEEPER_CONNECT", "zookeeper:2181")
            .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,PLAINTEXT_INTERNAL:PLAINTEXT")
            .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://localhost:9092,PLAINTEXT_INTERNAL://kafka:29092")
            .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092,PLAINTEXT_INTERNAL://0.0.0.0:29092")
            .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT_INTERNAL")
            //WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(KafkaPort))
            .WithNetwork(_network)// Use the same network as Zookeeper
            .WithNetworkAliases("kafka") // Set a network alias for Kafka
            .Build();

        _schemaRegistryContainer = new ContainerBuilder()
            .WithImage("confluentinc/cp-schema-registry:7.6.0")
            .WithName("schema-registry-container")
            .WithPortBinding(SchemaRegistryPort, true)
            .WithNetwork(_network)
            .WithNetworkAliases(SchemaRegistryAlias)
            .WithEnvironment(new Dictionary<string, string>
            {
                ["SCHEMA_REGISTRY_HOST_NAME"] = SchemaRegistryAlias,
                ["SCHEMA_REGISTRY_LISTENERS"] = "http://0.0.0.0:8081",
                ["SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS"] = "PLAINTEXT://localhost:9092"
            })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(SchemaRegistryPort))
            .DependsOn(_kafkaContainer)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _network.CreateAsync();
        await _kafkaContainer.StartAsync();
        await Task.Delay(5000); // Let Kafka fully settle
        await _schemaRegistryContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _schemaRegistryContainer.StopAsync();
        await _kafkaContainer.StopAsync();
        await _network.DeleteAsync();
    }

    public class OrderMessage
    {
        [JsonProperty("Order")]
        public object Order { get; set; }
    }

    public class StandardOrderMessage
    {
        [JsonProperty("type")]
        public string Type => "Standard";

        [JsonProperty("standardField")]
        public string StandardField { get; set; }
    }

    [Fact]
    public async Task CanProduceKafkaMessageWithJsonSchema()
    {
        string schemaRegistryUrl = "http://localhost:8081";

        // Generate JSON Schema
        var schema = JsonSchema.FromType<OrderMessage>();
        schema.Properties["Order"].AnyOf.Add(JsonSchema.FromType<StandardOrderMessage>());

        string schemaJson = schema.ToJson();

        // Register schema manually
        int schemaId;
        using (var client = new HttpClient())
        {
            var payload = new
            {
                schemaType = "JSON",
                schema = schemaJson
            };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/vnd.schemaregistry.v1+json");
            var response = await client.PostAsync($"{schemaRegistryUrl}/subjects/{Topic}-value/versions", content);
            var resultString = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Dictionary<string, int>>(resultString);
            schemaId = result["id"];
            Console.WriteLine($"✅ Registered schema ID: {schemaId}");
        }

        // Send message
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = "localhost:9092"
        };
        var registryConfig = new SchemaRegistryConfig
        {
            Url = schemaRegistryUrl
        };
        var serializerConfig = new JsonSerializerConfig
        {
            AutoRegisterSchemas = false
        };

        using var schemaRegistry = new CachedSchemaRegistryClient(registryConfig);
        var producer = new ProducerBuilder<string, OrderMessage>(producerConfig)
            .SetValueSerializer(new JsonSerializer<OrderMessage>(schemaRegistry, serializerConfig))
            .Build();

        var message = new OrderMessage
        {
            Order = new StandardOrderMessage { StandardField = "Test-123" }
        };

        var result2 = await producer.ProduceAsync(Topic, new Message<string, OrderMessage>
        {
            Key = Guid.NewGuid().ToString(),
            Value = message
        });

        Console.WriteLine($"✅ Message sent to: {result2.TopicPartitionOffset}");
    }
}
