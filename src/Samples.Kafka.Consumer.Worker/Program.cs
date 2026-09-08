using Confluent.Kafka;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConsumer<string, string>>(sp =>
{
    var config = new ConsumerConfig
    {
        BootstrapServers = "my-local-kafka-test.uksouth.azurecontainer.io:9092",
        GroupId = "my-consumer-group",
        AutoOffsetReset = AutoOffsetReset.Earliest
    };
    return new ConsumerBuilder<string, string>(config).Build();
});

builder.Services.AddHostedService<KafkaConsumerWorker>();



var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapGet("/", () => "Hello World!");

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.Run();



class KafkaConsumerWorker : BackgroundService
{
    private readonly ILogger<KafkaConsumerWorker> _logger;
    private readonly IConsumer<string, string> _consumer;
    public KafkaConsumerWorker(ILogger<KafkaConsumerWorker> logger, IConsumer<string, string> consumer)
    {
        _logger = logger;
        _consumer = consumer;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka consumer worker started.");
        _consumer.Subscribe("my-topic");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);
                if (consumeResult != null)
                {
                    _logger.LogInformation($"Consumed message '{consumeResult.Message.Value}' at: '{consumeResult.TopicPartitionOffset}'.");
                }
            }
            catch (ConsumeException e)
            {
                _logger.LogError($"Consume error: {e.Error.Reason}");
            }
        }
    }
    public override void Dispose()
    {
        _consumer.Close();
        base.Dispose();
    }
}