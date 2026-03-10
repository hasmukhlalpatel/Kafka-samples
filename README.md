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

sample2

```yaml
---
networks:
  my_network:

services:
  broker:
    image: confluentinc/confluent-local:7.6.1
    hostname: broker
    container_name: broker
    networks:
      - my_network
    ports:
      - "9092:9092"
      - "9101:9101"
    environment:
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: 'CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT'
      KAFKA_ADVERTISED_LISTENERS: 'PLAINTEXT://broker:29092,PLAINTEXT_HOST://localhost:9092'
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS: 0
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      KAFKA_JMX_PORT: 9101
      KAFKA_JMX_HOSTNAME: localhost
      KAFKA_PROCESS_ROLES: 'broker,controller'
      KAFKA_CONTROLLER_QUORUM_VOTERS: '1@broker:29093'
      KAFKA_LISTENERS: 'PLAINTEXT://broker:29092,CONTROLLER://broker:29093,PLAINTEXT_HOST://0.0.0.0:9092'
      KAFKA_INTER_BROKER_LISTENER_NAME: 'PLAINTEXT'
      KAFKA_-localCONTROLLER_LISTENER_NAMES: 'CONTROLLER'
      KAFKA_LOG_DIRS: '/tmp/kraft-combined-logs'
      KAFKA_AUTO_CREATE_TOPICS_ENABLE: 'true'
      # Replace CLUSTER_ID with a unique base64 UUID using "bin/kafka-storage.sh random-uuid"
      # See https://docs.confluent.io/kafka/operations-tools/kafka-tools.html#kafka-storage-sh
      CLUSTER_ID: 'MkU3OEVBNTcwNTJENDM2Qk'

  schema-registry:
    image: confluentinc/cp-schema-registry:7.6.1
    hostname: schema-registry
    container_name: schema-registry
    networks:
      - my_network
    depends_on:
      - broker
    ports:
      - "8081:8081"
    environment:
      SCHEMA_REGISTRY_HOST_NAME: schema-registry
      SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS: 'broker:29092'
      SCHEMA_REGISTRY_LISTENERS: http://0.0.0.0:8081

  control-center:
    image: confluentinc/cp-enterprise-control-center:7.6.1
    hostname: control-center
    container_name: control-center
    depends_on:
      - broker
      - schema-registry
    networks:
      - my_network
    ports:
      - "9021:9021"
    environment:
      CONTROL_CENTER_BOOTSTRAP_SERVERS: 'broker:29092'
      CONTROL_CENTER_SCHEMA_REGISTRY_URL: "http://schema-registry:8081"
      CONTROL_CENTER_REPLICATION_FACTOR: 1
      CONTROL_CENTER_INTERNAL_TOPICS_PARTITIONS: 1
      CONTROL_CENTER_MONITORING_INTERCEPTOR_TOPIC_PARTITIONS: 1
      CONFLUENT_METRICS_TOPIC_REPLICATION: 1
      PORT: 9021

  rest-proxy:
    image: confluentinc/cp-kafka-rest:7.5.0
    depends_on:
      - broker
      - schema-registry
    networks:
      - my_network
    ports:
      - "8082:8082"
    hostname: rest-proxy
    container_name: rest-proxy
    environment:
      KAFKA_REST_HOST_NAME: rest-proxy
      KAFKA_REST_BOOTSTRAP_SERVERS: 'broker:29092'
      KAFKA_REST_LISTENERS: "http://0.0.0.0:8082"
      KAFKA_REST_SCHEMA_REGISTRY_URL: 'http://schema-registry:8081'
```

## This is a comprehensive observability + Kafka stack combining the full LGTM stack (Loki, Grafana, Tempo, Prometheus) with OpenTelemetry Collector and Confluent Kafka in KRaft mode.

```yaml
services:
  ### OBSERVABILITY ###
  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    container_name: otel-collector
    ports:
      - "4316:4316"   # OTLP gRPC
      - "9999:9999"   # Prometheus metrics endpoint (exporter)
    volumes:
      - ./configuration/otel-collector.yaml:/etc/otelcol/otel-collector.yaml:ro
    command: ["--config=/etc/otelcol/otel-collector.yaml"]
    networks:
      - local-network

  loki:
    image: grafana/loki:latest
    container_name: loki
    ports:
      - "3100:3100"
    volumes:
      - ./configuration/loki.yaml:/etc/loki/loki.yaml:ro
    command: -config.file=/etc/loki/loki.yaml
    networks:
      - local-network

  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./configuration/prometheus.yml:/etc/prometheus/prometheus.yml:ro
    networks:
      - local-network

  tempo:
    image: grafana/tempo:latest
    container_name: tempo
    ports:
      - "3200:3200"
      - "4317:4317"
    volumes:
      - ./configuration/tempo.yaml:/etc/tempo/tempo.yaml:ro
    command: ["-config.file=/etc/tempo/tempo.yaml"]
    networks:
      - local-network

  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    ports:
      - "3000:3000"
    volumes:
      - ./configuration/grafana/provisioning:/etc/grafana/provisioning
    depends_on:
      - loki
      - prometheus
      - tempo
    environment:
      - GF_AUTH_ANONYMOUS_ENABLED=true
      - GF_AUTH_ANONYMOUS_ORG_ROLE=Admin
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
      KAFKA_LISTENERS: PLAINTEXT://broker:9092,PLAINTEXT_HOST://0.0.0.0:9093,CONTROLLER://broker:9094
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT,CONTROLLER:PLAINTEXT
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://broker:9092,PLAINTEXT_HOST://localhost:9093
      KAFKA_CONTROLLER_QUORUM_VOTERS: '1@broker:9094'
      KAFKA_INTER_BROKER_LISTENER_NAME: 'PLAINTEXT'
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      KAFKA_AUTO_CREATE_TOPICS_ENABLE: true
    networks:
      - local-network

  control-center:
    image: confluentinc/cp-enterprise-control-center:latest
    container_name: control-center
```
