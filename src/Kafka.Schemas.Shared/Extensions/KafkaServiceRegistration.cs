using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kafka.Schemas.Shared.Extensions;

public static class KafkaHostingExtensions
{
    public static IServiceCollection AddKafkaServices( this IServiceCollection services,
        Action<ProducerConfig> producerConfigAction,
        Action<ConsumerConfig> consumerConfigAction)
    {
        var producerConfig = new ProducerConfig();
        var consumerConfig = new ConsumerConfig();
        producerConfigAction(producerConfig);
        consumerConfigAction(consumerConfig);
        services.AddSingleton(producerConfig);
        services.AddSingleton(consumerConfig);
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
        return AddKafkaServices(services, 
            (producerConfig) => producerSection.Bind(producerConfig),
            (consumerConfig) => consumerSection.Bind(consumerConfig));
    }
    public static IServiceCollection AddKafka( this IServiceCollection services,
        ProducerConfig producerConfig,
        ConsumerConfig consumerConfig)
    {
        services.AddSingleton(producerConfig);
        services.AddSingleton(consumerConfig);
        return services;
    }

    public static IServiceCollection AddKafka( this IServiceCollection services, IConfiguration config)
    {
        var producerSection = config.GetSection("kafka:producer");
        var consumerSection = config.GetSection("kafka:consumer");

        var producerConfig = new ProducerConfig();
        var consumerConfig = new ConsumerConfig();

        producerSection.Bind(producerConfig);
        consumerSection.Bind(consumerConfig);

        services.AddSingleton(producerConfig);
        services.AddSingleton(consumerConfig);
        return services;
    }
}
