using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaProducer.WebApp.Tests
{
    public class KafkaIntegrationTests : IAsyncLifetime
    {
        private WebApplicationFactory<KafkaProducerWebApp.Program> _factory;
        private IContainer _kafkaContainer;
        private IContainer _zookeeper;
        private IContainer _schemaRegistryContainer;

        private const int KafkaPort = 9092;
        private const int SchemaRegistryPort = 8081;

        private string BootstrapServers => "localhost:9092";
        private string SchemaRegistryUrl => "http://localhost:8081";

        DotNet.Testcontainers.Networks.INetwork _network;

        // Testcontainers configurations
        private const string zookeeperImage = "confluentinc/cp-zookeeper:7.6.0"; // Use a specific version
        private const string KafkaImage = "confluentinc/cp-kafka:7.6.0"; // Use a specific version
        private const string SchemaRegistryImage = "confluentinc/cp-schema-registry:7.6.0"; // Use a specific version

        public async Task InitializeAsync()
        {
            _network = new NetworkBuilder()
                .WithName("kafka-network")
                .Build();

            // 1. Start Zookeeper Container
            _zookeeper = BuildZookeeper();

            // 2. Start Kafka Container
            _kafkaContainer = BuildKafkaBroker();

            // 3. Start Schema Registry Container
            _schemaRegistryContainer = StartRegistryContainer();

            await Task.WhenAll(
                _zookeeper.StartAsync(),
                _kafkaContainer.StartAsync(),
                _schemaRegistryContainer.StartAsync()
            );

            // Get the dynamically assigned Kafka port
            var kafkaBootstrapServers = $"localhost:{_kafkaContainer.GetMappedPublicPort(9092)}";

            Console.WriteLine($"Kafka running on: {kafkaBootstrapServers}");

            // Get the dynamically assigned Schema Registry port
            var schemaRegistryUrl = $"http://localhost:{_schemaRegistryContainer.GetMappedPublicPort(8081)}";
            Console.WriteLine($"Schema Registry running on: {schemaRegistryUrl}");

            // 3. Configure WebApplicationFactory
            _factory = new WebApplicationFactory<KafkaProducerWebApp.Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((context, config) =>
                    {
                        // Override app settings with Testcontainers dynamic ports
                        var inMemorySettings = new Dictionary<string, string>
                        {
                        {"Kafka:BootstrapServers", kafkaBootstrapServers},
                        {"SchemaRegistry:Url", schemaRegistryUrl}
                        };
                        config.AddInMemoryCollection(inMemorySettings);
                    });
                    // Ensure the test host uses the correct environment, e.g., Development
                    builder.UseEnvironment("Development");
                });
        }
        private IContainer BuildZookeeper()
        {
            return new ContainerBuilder()
            .WithName("zookeeper")
            .WithImage(zookeeperImage)
            .WithPortBinding(2181, true)
            .WithEnvironment("ZOOKEEPER_CLIENT_PORT", "2181")
            .WithEnvironment("ZOOKEEPER_TICK_TIME", "2000")
            .WithNetwork(_network) // Use the same network as Kafka
            .Build();
        }
        private IContainer BuildKafkaBroker()
        {
            return new ContainerBuilder()
            .WithName("kafka-test")
            .WithImage(KafkaImage)
            .WithPortBinding(KafkaPort, true)
            .WithEnvironment("KAFKA_BROKER_ID", "1")
            .WithEnvironment("KAFKA_ZOOKEEPER_CONNECT", "zookeeper:2181")
            .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092")
            .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://localhost:9092")
            .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
            .DependsOn(_zookeeper)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(KafkaPort))
            .WithNetwork(_network)// Use the same network as Zookeeper
            .Build();
        }

        private IContainer StartRegistryContainer()
        {
            return new ContainerBuilder()
            .WithName("schema-registry-test")
            .WithImage(SchemaRegistryImage)
            .WithPortBinding(SchemaRegistryPort, true)
            .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "schema-registry")
            .WithEnvironment("SCHEMA_REGISTRY_LISTENERS", "http://0.0.0.0:8081")
            .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", "PLAINTEXT://localhost:9092")
            .DependsOn(_kafkaContainer)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(SchemaRegistryPort))
                .WithNetwork(_network) // Use the same network as Kafka
                .Build();
        }

        public async Task DisposeAsync()
        {
            // Clean up containers and factory
            await _factory.DisposeAsync();
            await _schemaRegistryContainer.DisposeAsync();
            await _kafkaContainer.DisposeAsync();
            await _network.DisposeAsync();
        }

        [Fact]
        public async Task Application_CanProduceAndConsumeKafkaMessage()
        {
            // Arrange
            var kafkaService = _factory.Services.GetRequiredService<KafkaProducerWebApp.KafkaService>();
            var topic = "test-topic";
            var key = "test-key";
            var value = "Hello Kafka from Testcontainers!";

            // Act - Produce a message
            var deliveryResult = await kafkaService.ProduceMessage(topic, key, value);

            // Assert - Message was produced successfully
            Assert.NotNull(deliveryResult);
            Assert.Equal(value, deliveryResult.Message.Value);

            // Act - Consume the message
            var consumedValue = kafkaService.ConsumeMessage(topic);

            // Assert - Message was consumed successfully
            Assert.Equal(value, consumedValue);
        }

        [Fact]
        public async Task Application_CanRegisterSchemaWithSchemaRegistry()
        {
            // Arrange
            var kafkaService = _factory.Services.GetRequiredService<KafkaProducerWebApp.KafkaService>();
            var subject = "test-schema-subject";
            var schema = "{\"type\":\"string\"}"; // A simple Avro schema

            // Act
            var schemaId = await kafkaService.RegisterSchema(subject, schema);

            // Assert
            Assert.True(schemaId > 0); // A positive schema ID indicates successful registration
        }
    }
}
