// KafkaProducerApp/SchemaGenerator.cs
using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;

namespace KafkaProducerApp
{
    public class SchemaGenerator
    {
        public static NJsonSchema.JsonSchema GenerateSchema<T>() => NewtonsoftJsonSchemaGenerator.FromType<T>(new NewtonsoftJsonSchemaGeneratorSettings
        {
            SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings
            {
                //TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                //Formatting = Newtonsoft.Json.Formatting.Indented,
                //Converters = [new Samples.Serialization.Library.PolymorphicJsonConverter<T>()]

                Formatting = Newtonsoft.Json.Formatting.None,

            }
        });

        public static string GenerateSchemaJson<T>() => GenerateSchema<T>().ToJson();
        public static NJsonSchema.JsonSchema GenerateSchema(Type type) => NewtonsoftJsonSchemaGenerator.FromType(type, new NewtonsoftJsonSchemaGeneratorSettings
        {
            SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings
            {
                //TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                //Formatting = Newtonsoft.Json.Formatting.Indented,
                //Converters = [new Samples.Serialization.Library.PolymorphicJsonConverter<T>()]

                Formatting = Newtonsoft.Json.Formatting.None,

            }
        });
        public static string GenerateSchemaJson(Type type) => GenerateSchema(type).ToJson();

        public static NJsonSchema.JsonSchema GenerateSchemaWithTypeName<T>() => NewtonsoftJsonSchemaGenerator.FromType<T>(new NewtonsoftJsonSchemaGeneratorSettings
        {
            SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                Formatting = Newtonsoft.Json.Formatting.Indented,
                //Converters = [new Samples.Serialization.Library.PolymorphicJsonConverter<T>()]

            }
        });
        public static string GenerateSchemaJsonWithTypeName<T>() => GenerateSchemaWithTypeName<T>().ToJson();

        public static NJsonSchema.JsonSchema GenerateSchemaWithTypeName(Type type) => NewtonsoftJsonSchemaGenerator.FromType(type, new NewtonsoftJsonSchemaGeneratorSettings
        {
            SerializerSettings = new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                Formatting = Newtonsoft.Json.Formatting.Indented,
                //Converters = [new Samples.Serialization.Library.PolymorphicJsonConverter<T>()]

            }
        });

        public static string GenerateSchemaJsonWithTypeName(Type type) => GenerateSchemaWithTypeName(type).ToJson();

        //public static string GenerateSchema2<T>()
        //{
        //    var generator = new JSchemaGenerator
        //    {
        //        // Settings for the JSchemaGenerator instance
        //        // Note: SerializerSettings are applied to the internal JsonSerializer used by the generator
        //        // when converting the .NET type to JSchema.
        //        SerializerSettings = new JsonSerializerSettings
        //        {
        //            // Removed TypeNameHandling.Auto to prevent $type property in generated JSON
        //            // which is not expected by the 'anyOf' schema.
        //            // TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto, // REMOVED
        //            Formatting = Formatting.None, // Use None for embedding into the root schema string
        //            //Converters = [new Samples.Serialization.Library.PolymorphicJsonConverter<T>()]
        //        }
        //    };
        //    return generator.Generate<T>().ToJson();
        //}
    }
}
