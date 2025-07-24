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
        private IContainer _schemaRegistryContainer;
        DotNet.Testcontainers.Networks.INetwork _network;

        // Testcontainers configurations
        private const string KafkaImage = "confluentinc/cp-kafka:7.6.0"; // Use a specific version
        private const string SchemaRegistryImage = "confluentinc/cp-schema-registry:7.6.0"; // Use a specific version

        public async Task InitializeAsync()
        {
            _network = new NetworkBuilder()
                .WithName("kafka-network")
                .Build();
            // 1. Start Kafka Container
            _kafkaContainer = new ContainerBuilder()
                .WithImage(KafkaImage)
                .WithPortBinding(9092, true) // Bind Kafka's internal port to a random host port
                .WithPortBinding(9093, true) // For external listeners (often used with Schema Registry)
                .WithEnvironment("KAFKA_BROKER_ID", "1")
                .WithEnvironment("KAFKA_ZOOKEEPER_CONNECT", "localhost:2181") // Zookeeper is often bundled or assumed for Kafka
                .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092,PLAINTEXT_HOST://0.0.0.0:9093")
                .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", $"PLAINTEXT://localhost:{{_kafkaContainer.Get  MappedPort(9092)}},PLAINTEXT_HOST://localhost:{{_kafkaContainer.GetMappedPort(9093)}}")
                .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT")
                .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(9092))
                .WithNetwork(_network) // Use a custom network for Kafka
                .Build();

            await _kafkaContainer.StartAsync();

            // Get the dynamically assigned Kafka port
            var kafkaBootstrapServers = $"localhost:{_kafkaContainer.GetMappedPublicPort(9092)}";
            Console.WriteLine($"Kafka running on: {kafkaBootstrapServers}");

            // 2. Start Schema Registry Container
            _schemaRegistryContainer = new ContainerBuilder()
                .WithImage(SchemaRegistryImage)
                .WithPortBinding(8081, true) // Bind Schema Registry's internal port to a random host port
                .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "localhost")
                .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", kafkaBootstrapServers) // Link to Kafka
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8081))
                .WithNetwork(_network) // Use the same network as Kafka
                .Build();

            await _schemaRegistryContainer.StartAsync();

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

        public async Task DisposeAsync()
        {
            // Clean up containers and factory
            await _factory.DisposeAsync();
            if (_schemaRegistryContainer != null)
            {
                await _schemaRegistryContainer.DisposeAsync();
            }
            if (_kafkaContainer != null)
            {
                await _kafkaContainer.DisposeAsync();
            }
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
