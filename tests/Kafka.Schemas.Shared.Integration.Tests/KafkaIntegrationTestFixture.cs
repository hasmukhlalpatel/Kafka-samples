using Confluent.Kafka;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Kafka.Schemas.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kafka.Schemas.Shared.Integration.Tests;

public class KafkaIntegrationTestFixture : IAsyncLifetime
{
    private const string KafkaContainerAlias = "kafka";
    private const string SchemaRegistryAlias = "schema-registry";
    private const int KafkaPort = 9092;
    private const int SchemaRegistryPort = 8081;

    private readonly INetwork _network;
    private readonly IContainer _kafkaContainer;
    private readonly IContainer _schemaRegistryContainer;
    private const string KafkaImage = "confluentinc/cp-kafka:7.6.0"; // Use a specific version
    private const string SchemaRegistryImage = "confluentinc/cp-schema-registry:7.6.0"; // Use a specific version

    public IServiceProvider Services { get; private set; }
    public IProducer<string, string> Producer { get; private set; }
    public IConsumer<string, string> Consumer { get; private set; }
    public string TopicName { get; } = "env-test-topic";
    public KafkaIntegrationTestFixture()
    {
        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();

        _kafkaContainer = new ContainerBuilder(KafkaImage)
            .WithName("kafka-test")
            .WithPortBinding(KafkaPort, true)
            .WithEnvironment("KAFKA_ENABLE_KRAFT", "yes")
            .WithEnvironment("KAFKA_CFG_PROCESS_ROLES", "broker")
            .WithEnvironment("KAFKA_CFG_NODE_ID", "1")
            .WithEnvironment("KAFKA_CFG_CONTROLLER_QUORUM_VOTERS", "1@kafka-test:9093")
            .WithEnvironment("KAFKA_CFG_LISTENERS", "PLAINTEXT://:9092,CONTROLLER://:9093")
            .WithEnvironment("KAFKA_CFG_ADVERTISED_LISTENERS", "PLAINTEXT://localhost:9092")
            .WithEnvironment("KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,CONTROLLER:PLAINTEXT")
            .WithEnvironment("ALLOW_PLAINTEXT_LISTENER", "yes")
            .WithNetwork(_network)
            .WithNetworkAliases(KafkaContainerAlias)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(KafkaPort))
            .Build();


        _schemaRegistryContainer = new ContainerBuilder(SchemaRegistryImage)
            .WithName("schema-registry-container")
            .WithPortBinding(SchemaRegistryPort, true)
            .WithNetwork(_network)
            .WithNetworkAliases(SchemaRegistryAlias)
            .WithEnvironment(new Dictionary<string, string>
            {
                ["SCHEMA_REGISTRY_HOST_NAME"] = SchemaRegistryAlias,
                ["SCHEMA_REGISTRY_LISTENERS"] = "http://0.0.0.0:8081",
                ["SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS"] = "PLAINTEXT://kafka-test:9092"
            })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(SchemaRegistryPort))
            .DependsOn(_kafkaContainer)
            .Build();


        // Set env vars for test
        Environment.SetEnvironmentVariable("kafka__producer__bootstrapservers", "localhost:9092");
        Environment.SetEnvironmentVariable("kafka__consumer__bootstrapservers", "localhost:9092");
        Environment.SetEnvironmentVariable("kafka__consumer__groupid", "env-test-group");

        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        
        var services = new ServiceCollection();
        services.AddKafkaServices(config);

        Services = services.BuildServiceProvider();

        Producer = Services.GetRequiredService<IProducer<string, string>>();
        Consumer = Services.GetRequiredService<IConsumer<string, string>>();

        Consumer.Subscribe(TopicName);
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
}
