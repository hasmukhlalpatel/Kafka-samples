using Confluent.SchemaRegistry.Serdes;

namespace Kafka.Schemas.Shared.Serialization
{
    static internal  class DefaultSerializerConfig
    {
        public static JsonSerializerConfig SerializerConfig { get; private set; }
        public static JsonDeserializerConfig DeserializerConfig { get; private set; }
        static DefaultSerializerConfig()
        {
            SerializerConfig = new JsonSerializerConfig
            {
                AutoRegisterSchemas = false, // Set this back to true for auto-registration
                UseLatestVersion = true,
                LatestCompatibilityStrict = true,
                Validate = false, // Set this back to true for validation
            };

            DeserializerConfig = new JsonDeserializerConfig
            {
                UseLatestVersion = true,
                Validate = false, // Set this back to true for validation
            };
        }
    }
}
