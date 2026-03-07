var builder = DistributedApplication.CreateBuilder(args);
//var kafka = builder.AddKafka("kafka");
// Create a custom network for Kafka infrastructure
// Add Zookeeper container
var zookeeper = builder.AddContainer("zookeeper", "confluentinc/cp-zookeeper")
    .WithImage("confluentinc/cp-zookeeper:7.6.0")
    .WithEndpoint(port: 2181, targetPort: 2181, name: "zookeeper-port")
    .WithEnvironment("ZOOKEEPER_CLIENT_PORT", "2181")
    .WithEnvironment("ZOOKEEPER_TICK_TIME", "2000")
    .WithContainerNetworkAlias("zookeeper");

// Add Kafka container
var kafka = builder.AddContainer("kafka", "confluentinc/cp-kafka")
    .WithImage("confluentinc/cp-kafka:7.6.0")
    .WithEndpoint(port:9292, targetPort: 9092, name: "kafka-plaintext-host")
    .WithEndpoint(port:29292, targetPort: 29092, name: "kafka-internal")
    .WithEnvironment("KAFKA_BROKER_ID", "1")
    .WithEnvironment("KAFKA_ZOOKEEPER_CONNECT", "zookeeper:2181")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://localhost:29092,PLAINTEXT_HOST://localhost:9092")
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT")
    .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS", "0")
    .WithContainerNetworkAlias("kafka")
    .WaitFor(zookeeper);

// Add Schema Registry container
var schemaRegistry = builder.AddContainer("schema-registry", "confluentinc/cp-schema-registry")
    .WithImage("confluentinc/cp-schema-registry:7.6.0")
    .WithEndpoint(port:8081, targetPort: 8081, name: "schema-registry-port")
    .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "schema-registry")
    .WithEnvironment("SCHEMA_REGISTRY_LISTENERS", "http://0.0.0.0:8081")
    .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", "localhost:9092")
    .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_SECURITY_PROTOCOL", "PLAINTEXT")
    .WithContainerNetworkAlias("schema-registry")
    .WaitFor(kafka);

// Add projects
builder.AddProject<Projects.KafkaProducer_WebApp>("kafkaproducer-webapp")
    .WaitFor(schemaRegistry);

builder.AddProject<Projects.KafkaConsumerApp>("kafkaconsumerapp")
    .WaitFor(schemaRegistry);

builder.Build().Run();