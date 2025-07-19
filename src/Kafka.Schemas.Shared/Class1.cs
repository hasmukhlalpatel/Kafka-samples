namespace Kafka.Schemas.Shared
{
    public class KafkaConfig
    {
        public string BootstrapServers { get; private set; }
        public string SchemaRegistryUrl { get; private set; }
        public string TopicName { get; private set; }
        public string ProducerGroupId { get; private set; }
        public string ConsumerGroupId { get; private set; }

        // Static default instance of KafkaConfig
        public static KafkaConfig Default { get; } = new KafkaConfig
        {
            BootstrapServers = "localhost:9092",
            SchemaRegistryUrl = "http://localhost:8081",
            TopicName = "sample-app",
            ProducerGroupId = "sample-app-producer-group",
            ConsumerGroupId = "sample-app-consumer-group"
        };

        // Private constructor to prevent direct instantiation without values
        // unless using the object initializer for the Default instance.
        private KafkaConfig() { }

        // Optional: Public constructor if you want to allow custom configurations
        public KafkaConfig(string bootstrapServers, string schemaRegistryUrl, string topicName, string producerGroupId, string consumerGroupId)
        {
            BootstrapServers = bootstrapServers;
            SchemaRegistryUrl = schemaRegistryUrl;
            TopicName = topicName;
            ProducerGroupId = producerGroupId;
            ConsumerGroupId = consumerGroupId;
        }
    }
}
