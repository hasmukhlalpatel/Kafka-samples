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
    private const int KafkaPort = 9092;
    private const int SchemaRegistryPort = 8081;

    private INetwork _network;
    private IContainer _kafkaContainer;
    private IContainer _schemaRegistryContainer;

    public IServiceProvider Services { get; private set; }
    public IProducer<string, string> Producer { get; private set; }
    public IConsumer<string, string> Consumer { get; private set; }
    public string TopicName { get; } = "env-test-topic";
    public KafkaIntegrationTestFixture()
    {
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

        SetupContainers();

        Producer = Services.GetRequiredService<IProducer<string, string>>();
        Consumer = Services.GetRequiredService<IConsumer<string, string>>();

        Consumer.Subscribe(TopicName);
    }

    private void SetupContainers()
    {
        _network = new NetworkBuilder()
            .WithName(Guid.NewGuid().ToString("D"))
            .Build();
        _kafkaContainer = BuildKafkaContainer();
        _schemaRegistryContainer = BuildSchemaRegistryContainer();
    }

    private IContainer BuildSchemaRegistryContainer()
    {
        return new ContainerBuilder("confluentinc/cp-schema-registry:latest")
            .WithName("schema-registry-test")
            .WithHostname("schema-registry")
            .WithNetwork(_network)
            .WithNetworkAliases("schema-registry")
            .WithPortBinding(8081, 8081)
            .WithEnvironment("SCHEMA_REGISTRY_HOSTNAME", "schema-registry")
            .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "schema-registry")
            .WithEnvironment("SCHEMA_REGISTRY_LISTENERS", "http://0.0.0.0:8081")
            .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", "kafka:29092")
            .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_SECURITY_PROTOCOL", "PLAINTEXT")
            .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_CLUSTER_ID", "YzkwZTdmNTYtNGF1ZC00NW")
            .DependsOn(_kafkaContainer)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(8081))
            .Build();
    }

    private IContainer BuildKafkaContainer()
    {
        return new ContainerBuilder("confluentinc/cp-kafka:latest")
            .WithName("kafka-test")
            .WithHostname("kafka")
            .WithNetwork(_network)
            .WithNetworkAliases("kafka")
            .WithPortBinding(9092, 9092)
            .WithPortBinding(9093, 9093)
            .WithEnvironment("CLUSTER_ID", "YzkwZTdmNTYtNGF1ZC00NW")
            .WithEnvironment("KAFKA_NODE_ID", "1")
            .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
            .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
            .WithEnvironment("KAFKA_LISTENERS",
                "PLAINTEXT://kafka:29092," +
                "PLAINTEXT_HOST://0.0.0.0:9092," +
                "CONTROLLER://kafka:29093")
            .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP",
                "PLAINTEXT:PLAINTEXT," +
                "PLAINTEXT_HOST:PLAINTEXT," +
                "CONTROLLER:PLAINTEXT")
            .WithEnvironment("KAFKA_ADVERTISED_LISTENERS",
                "PLAINTEXT://kafka:29092," +
                "PLAINTEXT_HOST://localhost:9092")
            .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@kafka:29093")
            .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
            .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
            .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "1")
            .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1")
            .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(9092))
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
}
