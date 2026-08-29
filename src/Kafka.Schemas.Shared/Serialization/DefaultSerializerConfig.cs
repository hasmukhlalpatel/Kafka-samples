using Confluent.Kafka;
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
        public static object GetDefaultSerializer(Type type)
        {
            if (defaultSerializers.TryGetValue(type, out var serializer))
            {
                return serializer;
            }
            throw new NotSupportedException($"No default serializer found for type {type.FullName}");
        }
        public static bool TryGetDefaultSerializer(Type type, out object serializer)
        {
            if (defaultSerializers.TryGetValue(type, out serializer))
            {
                return true;
            }
            return false;
        }

        private static readonly Dictionary<Type, object> defaultSerializers = new Dictionary<Type, object>
        {
            {
                typeof(Null),
                Serializers.Null
            },
            {
                typeof(int),
                Serializers.Int32
            },
            {
                typeof(long),
                Serializers.Int64
            },
            {
                typeof(string),
                Serializers.Utf8
            },
            {
                typeof(float),
                Serializers.Single
            },
            {
                typeof(double),
                Serializers.Double
            },
            {
                typeof(byte[]),
                Serializers.ByteArray
            }
        };

        public static object GetDefaultDeserializer(Type type)
        {
            if (defaultDeserializers.TryGetValue(type, out var deserializer))
            {
                return deserializer;
            }
            throw new NotSupportedException($"No default deserializer found for type {type.FullName}");
        }
        public static bool TryGetDefaultDeserializer(Type type, out object deserializer)
        {
            if (defaultDeserializers.TryGetValue(type, out deserializer))
            {
                return true;
            }
            return false;
        }
        private static readonly Dictionary<Type, object> defaultDeserializers = new Dictionary<Type, object>
        {
            {
                typeof(Null),
                Deserializers.Null
            },
            {
                typeof(int),
                Deserializers.Int32
            },
            {
                typeof(long),
                Deserializers.Int64
            },
            {
                typeof(string),
                Deserializers.Utf8
            },
            {
                typeof(float),
                Deserializers.Single
            },
            {
                typeof(double),
                Deserializers.Double
            },
            {
                typeof(byte[]),
                Deserializers.ByteArray
            }
        };
    }
}
