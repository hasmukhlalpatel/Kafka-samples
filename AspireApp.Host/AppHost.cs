var builder = DistributedApplication.CreateBuilder(args);

var testConnectionString = builder.AddConnectionString("TestDB");

// Add Kafka container
var kafka = builder.AddContainer("kafka", "confluentinc/cp-kafka")
    .WithImage("confluentinc/cp-kafka:latest")
    .WithEndpoint(port: 9092, targetPort: 9092, name: "kafka-plaintext-host")
    .WithEndpoint(port: 29092, targetPort: 29092, name: "kafka-internal")
      .WithEnvironment("CLUSTER_ID", "YzkwZTdmNTYtNGF1ZC00NW")
      .WithEnvironment("KAFKA_NODE_ID", "1")
      .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
      .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
      .WithEnvironment("KAFKA_LISTENERS", " PLAINTEXT://kafka:29092,PLAINTEXT_HOST://0.0.0.0:9092,CONTROLLER://kafka:29093")
      .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT,CONTROLLER:PLAINTEXT")
      .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://kafka:29092,PLAINTEXT_HOST://localhost:9092")
      .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@kafka:29093")
      .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
      .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
      .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "1")
      .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1")
      .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", " true")
    .WithContainerNetworkAlias("kafka");

// Add Schema Registry container
var schemaRegistry = builder.AddContainer("schema-registry", "confluentinc/cp-schema-registry")
    .WithImage("confluentinc/cp-schema-registry:latest")
    .WithEndpoint(port: 8081, targetPort: 8081, name: "schema-registry-port")
    .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "schema-registry")
    .WithEnvironment("SCHEMA_REGISTRY_LISTENERS", "http://0.0.0.0:8081")
    .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", "kafka:29092")
    .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_SECURITY_PROTOCOL", "PLAINTEXT")
    .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_CLUSTER_ID", "YzkwZTdmNTYtNGF1ZC00NW")
    .WithContainerNetworkAlias("schema-registry")
    .WaitFor(kafka);

// Add projects
builder.AddProject<Projects.KafkaProducer_WebApp>("kafkaproducer-webapp")
    .WaitFor(schemaRegistry);

builder.AddProject<Projects.KafkaConsumerApp>("kafkaconsumerapp")
    .WaitFor(schemaRegistry);

builder.AddProject<Projects.Samples_Web_Api>("samples-web-api")
    .WithReference(testConnectionString)
    ;
    //.WithHealthCheck("/health");

builder.Build().Run();