using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Kafka.Schemas.Shared.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kafka.Schemas.Shared.Extensions;

public static class KafkaHostingExtensions
{
    public static IServiceCollection AddKafkaServices( this IServiceCollection services,
        Func<ProducerConfig> producerConfigAction,
        Func<ConsumerConfig> consumerConfigAction,
        Func<SchemaRegistryConfig> schemaRegistryConfigAction)
    {
        services.AddSingleton(producerConfigAction());
        services.AddSingleton(consumerConfigAction());
        services.AddSingleton(schemaRegistryConfigAction());

        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var cfg = sp.GetRequiredService<ProducerConfig>();
            return new ProducerBuilder<string, string>(cfg).Build();
        });
        services.AddSingleton<IConsumer<string, string>>(sp =>
        {
            var cfg = sp.GetRequiredService<ConsumerConfig>();
            return new ConsumerBuilder<string, string>(cfg).Build();
        });
        return services;
    }

    public static IServiceCollection AddKafkaServices( this IServiceCollection services,
        IConfiguration config)
    {
        var producerSection = config.GetSection("kafka:producer");
        var consumerSection = config.GetSection("kafka:consumer");
        var schemaRegistrySection = config.GetSection("kafka:schemaRegistry");
        var producerConfig = new ProducerConfig();
        var consumerConfig = new ConsumerConfig();
        var schemaRegistryConfig = new SchemaRegistryConfig();

        producerSection.Bind(producerConfig);
        consumerSection.Bind(consumerConfig);
        schemaRegistrySection.Bind(schemaRegistryConfig);

        return AddKafkaServices(services, () => producerConfig,() => consumerConfig,() => schemaRegistryConfig);
    }
    public static IServiceCollection AddKafka(this IServiceCollection services,
        ProducerConfig producerConfig,
        ConsumerConfig consumerConfig,
        SchemaRegistryConfig schemaRegistryConfig)
    {
        services.AddSingleton(producerConfig);
        services.AddSingleton(consumerConfig);
        services.AddSingleton(schemaRegistryConfig);
        return services;
    }

    public static IServiceCollection AddKafka( this IServiceCollection services, IConfiguration config)
    {
        var producerSection = config.GetSection("kafka:producer");
        var consumerSection = config.GetSection("kafka:consumer");
        var schemaRegistrySection = config.GetSection("kafka:schemaRegistry");

        var producerConfig = new ProducerConfig();
        var consumerConfig = new ConsumerConfig();
        var schemaRegistryConfig = new SchemaRegistryConfig(); 

        producerSection.Bind(producerConfig);
        consumerSection.Bind(consumerConfig);
        schemaRegistrySection.Bind(schemaRegistryConfig);

        services.AddSingleton(producerConfig);
        services.AddSingleton(consumerConfig);
        services.AddSingleton(schemaRegistryConfig);
        return services;
    }
    public static IServiceCollection AddMessageProducer( this IServiceCollection services)
    {
        services.AddSingleton(typeof(IMessageProducer<,>),typeof(MessageProducer<,>));
        return services;
    }

    public static IServiceCollection AddMessageProducerWithDefaultSerializer<TKey, TValue>(this IServiceCollection services, int defaultSchemaId = 0)
        where TValue : class
    {
        var serializer = new CustomSerializer<TValue>(defaultSchemaId);
        services.AddSingleton<IMessageProducer<TKey, TValue>>(sp =>
        {
            var producerConfig = sp.GetRequiredService<ProducerConfig>();
            var logger = sp.GetRequiredService<ILogger<MessageProducer<TKey, TValue>>>();
            return new MessageProducer<TKey, TValue>(producerConfig, serializer, logger);
        });
        return services;
    }

    public static IServiceCollection AddMessageProducer<TKey, TValue>(this IServiceCollection services,
        IAsyncSerializer<TValue>? serializer = null)
        where TValue : class
    {
        services.AddSingleton<IMessageProducer<TKey, TValue>>(sp =>
        {
            var producerConfig = sp.GetRequiredService<ProducerConfig>();
            var logger = sp.GetRequiredService<ILogger<MessageProducer<TKey, TValue>>>();
            return new MessageProducer<TKey, TValue>(producerConfig, serializer, logger);
        });
        return services;
    }
    public static IServiceCollection AddMessageProducer<TKey, TValue>(this IServiceCollection services,
        ISerializer<TValue>? serializer = null)
        where TValue : class
    {
        services.AddSingleton<IMessageProducer<TKey, TValue>>(sp =>
        {
            var producerConfig = sp.GetRequiredService<ProducerConfig>();
            var logger = sp.GetRequiredService<ILogger<MessageProducer<TKey, TValue>>>();
            return new MessageProducer<TKey, TValue>(producerConfig, serializer, logger);
        });
        return services;
    }
    public static IServiceCollection AddMessageProducerWithSchemaRegistry<TKey, TValue>(this IServiceCollection services,
    JsonSerializerConfig? jsonSerializerConfig = null)
    where TValue : class
    {
        services.AddSingleton<IMessageProducer<TKey, TValue>>(sp =>
        {
            var producerConfig = sp.GetRequiredService<ProducerConfig>();
            var schemaRegistryConfig = sp.GetRequiredService<SchemaRegistryConfig>();
            var logger = sp.GetRequiredService<ILogger<MessageProducer<TKey, TValue>>>();
            return new MessageProducer<TKey, TValue>(producerConfig, schemaRegistryConfig, logger, jsonSerializerConfig);
        });
        return services;
    }

    public static IServiceCollection AddMessageConsumer(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IMessageConsumer<,>), typeof(MessageConsumer<,>));
        return services;
    }

    public static IServiceCollection AddMessageConsumerWithDefaultSerializer<TKey, TValue>(this IServiceCollection services)
    where TValue : class
    {
        var deserializer = new CustomDeserializer<TValue>();
        services.AddSingleton<IMessageConsumer<TKey, TValue>>(sp =>
        {
            var consumerConfig = sp.GetRequiredService<ConsumerConfig>();
            var logger = sp.GetRequiredService<ILogger<MessageConsumer<TKey, TValue>>>();
            return new MessageConsumer<TKey, TValue>(consumerConfig, deserializer, logger);
        });
        return services;
    }

    public static IServiceCollection AddMessageConsumer<TKey, TValue>(this IServiceCollection services,
    IAsyncDeserializer<TValue>? deserializer = null)
    where TValue : class
    {
        services.AddSingleton<IMessageConsumer<TKey, TValue>>(sp =>
        {
            var consumerConfig = sp.GetRequiredService<ConsumerConfig>();
            var logger = sp.GetRequiredService<ILogger<MessageConsumer<TKey, TValue>>>();
            return new MessageConsumer<TKey, TValue>(consumerConfig, deserializer, logger);
        });
        return services;
    }
    public static IServiceCollection AddMessageConsumer<TKey, TValue>(this IServiceCollection services,
        IDeserializer<TValue>? deserializer = null)
        where TValue : class
    {
        services.AddSingleton<IMessageConsumer<TKey, TValue>>(sp =>
        {
            var consumerConfig = sp.GetRequiredService<ConsumerConfig>();
            var logger = sp.GetRequiredService<ILogger<MessageConsumer<TKey, TValue>>>();
            return new MessageConsumer<TKey, TValue>(consumerConfig, deserializer, logger);
        });
        return services;
    }

    public static IServiceCollection AddMessageConsumerWithSchemaRegistry<TKey, TValue>(this IServiceCollection services,
    JsonSerializerConfig? jsonSerializerConfig = null)
    where TValue : class
    {
        services.AddSingleton<IMessageConsumer<TKey, TValue>>(sp =>
        {
            var consumerConfig = sp.GetRequiredService<ConsumerConfig>();
            var schemaRegistryConfig = sp.GetRequiredService<SchemaRegistryConfig>();
            var logger = sp.GetRequiredService<ILogger<MessageConsumer<TKey, TValue>>>();
            return new MessageConsumer<TKey, TValue>(consumerConfig, schemaRegistryConfig, logger, jsonSerializerConfig);
        });
        return services;
    }
}
