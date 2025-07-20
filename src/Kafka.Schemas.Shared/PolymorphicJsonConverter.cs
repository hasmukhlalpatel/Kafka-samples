using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kafka.Schemas.Shared
{
    public class PolymorphicJsonConverter : JsonConverter
    {
        private readonly Type[] _knownTypes;
        public PolymorphicJsonConverter()
        {
            _knownTypes = new Type[0];
        }

        public PolymorphicJsonConverter(Type type1)
        {
            _knownTypes = new[] { type1 };
        }
        public PolymorphicJsonConverter(Type type1, Type type2)
        {
            _knownTypes = new[] { type1, type2 };
        }
        public override bool CanConvert(Type typeToConvert)
        {
            return _knownTypes.Length == 0 || _knownTypes.Contains(typeToConvert);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var typeName = jsonObject["$type"]?.ToString();
            if (string.IsNullOrEmpty(typeName))
            {
                throw new JsonSerializationException("Type information is missing in the JSON object.");
            }
            if (_knownTypes.Length > 0 && !_knownTypes.Any(t => t.FullName == typeName))
            {
                throw new JsonSerializationException($"Type '{typeName}' is not known.");
            }
            var type = _knownTypes.SingleOrDefault(t => t.FullName == typeName);
            
            type = type ?? AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.FullName == typeName);
            if (type == null)
            {
                throw new JsonSerializationException($"Type '{typeName}' could not be found.");
            }
            try
            {
                var jsonContract = serializer.ContractResolver.ResolveContract(type);
                var converter = jsonContract.Converter;
                jsonContract.Converter = null; // Temporarily set to null to avoid infinite recursion
                var content=  jsonObject.ToObject(type, serializer);
                jsonContract.Converter = converter;
                return content;
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException($"Error deserializing type '{typeName}': {ex.Message}", ex);
            }
        }
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            var type = value.GetType();
            var jsonContract = serializer.ContractResolver.ResolveContract(type);
            var converter = jsonContract.Converter;
            jsonContract.Converter = null; // Temporarily set to null to avoid infinite recursion
            var jsonObject = JObject.FromObject(value, serializer);
            jsonObject["$type"] = type.FullName;
            jsonObject.WriteTo(writer);
            jsonContract.Converter = converter;
        }
    }
}
