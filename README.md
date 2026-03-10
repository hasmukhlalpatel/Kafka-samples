# Kafka-samples
Kafka samples

## Setup Kafka broker and  schema-registry with docker-compose

Docker/ Rancher

```bash
docker-compose up -d
```

Podman

```bash
podman compose up -d
```

## Working Aspire Sample with Zookeeper, Kafka broker and Schema-registry 
`Note:` Not need any kafka pacakges to run follwing sample, as it is using REST API of schema-registry to register and fetch schemas.
```
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
    .WithEndpoint(port: 9092, targetPort: 9092, name: "kafka-plaintext-host")
    .WithEndpoint(port: 29092, targetPort: 29092, name: "kafka-internal")
    .WithEnvironment("KAFKA_BROKER_ID", "1")
    .WithEnvironment("KAFKA_ZOOKEEPER_CONNECT", "zookeeper:2181")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://kafka:29092,PLAINTEXT_HOST://localhost:9092")
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT")
    .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS", "0")
    .WithContainerNetworkAlias("kafka")
    .WaitFor(zookeeper);

// Add Schema Registry container
var schemaRegistry = builder.AddContainer("schema-registry", "confluentinc/cp-schema-registry")
    .WithImage("confluentinc/cp-schema-registry:7.6.0")
    .WithEndpoint(port: 8081, targetPort: 8081, name: "schema-registry-port")
    .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "schema-registry")
    .WithEnvironment("SCHEMA_REGISTRY_LISTENERS", "http://0.0.0.0:8081")
    .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", "kafka:29092")
    .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_SECURITY_PROTOCOL", "PLAINTEXT")
    .WithContainerNetworkAlias("schema-registry")
    .WaitFor(kafka);
```

`Troubleshooting:`
```
netstat -ano | findstr :29
netstat -ano | findstr :92
netstat -ano | findstr :81
```

```yaml
networks:
    - local-network

### KAFKA ###
broker:
  image: confluentinc/cp-kafka:latest
  container_name: broker
  ports:
    - "9092:9092"
    - "9093:9093"
  environment:
    CLUSTER_ID: 'YzkwZTdmNTYtNGF1ZC00NW'
    KAFKA_NODE_ID: 1
    KAFKA_PROCESS_ROLES: broker,controller
    KAFKA_CONTROLLER_LISTENER_NAMES: CONTROLLER
    KAFKA_LISTENERS: PLAINTEXT://broker:29092,PLAINTEXT_HOST://0.0.0.0:9092,CONTROLLER://broker:29093
    KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT,CONTROLLER:PLAINTEXT
    KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://broker:29092,PLAINTEXT_HOST://localhost:9092
    KAFKA_CONTROLLER_QUORUM_VOTERS: '1@broker:29093'
    KAFKA_INTER_BROKER_LISTENER_NAME: 'PLAINTEXT'
    KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
    KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
    KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
    KAFKA_AUTO_CREATE_TOPICS_ENABLE: true
  networks:
    - local-network

schema-registry:
  image: confluentinc/cp-schema-registry:latest
  container_name: schema-registry
  ports:
    - "8081:8081"
  environment:
    SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS: PLAINTEXT://broker:29092
    SCHEMA_REGISTRY_HOST_NAME: localhost
    SCHEMA_REGISTRY_LISTENERS: http://0.0.0.0:8081
    SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_CLUSTER_ID: YzkwZTdmNTYtNGF1ZC00NW
  networks:
    - local-network

#control-center:
#  image: confluentinc/cp-enterprise-control-center:latest
```
