using Confluent.Kafka;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = "my-local-kafka-test.uksouth.azurecontainer.io:9092"
    };
    return new ProducerBuilder<string, string>(config).Build();
});

builder.Services.AddHostedService<KafkaProducerWorker>();



var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapGet("/", () => "Hello World!");

app.Run();


class KafkaProducerWorker : BackgroundService
{
    private readonly ILogger<KafkaProducerWorker> _logger;
    private readonly IProducer<string, string> _producer;
    public KafkaProducerWorker(ILogger<KafkaProducerWorker> logger, IProducer<string, string> producer)
    {
        _logger = logger;
        _producer = producer;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka producer worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messageValue = $"Message at {DateTime.UtcNow}";
                var message = new Message<string, string> { Key = Guid.NewGuid().ToString(), Value = messageValue };
                var produceResult = await _producer.ProduceAsync("my-topic", message, stoppingToken);
                _logger.LogInformation($"Produced message '{produceResult.Message.Value}' at: '{produceResult.TopicPartitionOffset}'.");
                await Task.Delay(500, stoppingToken); // Simulate some processing time
            }
            catch (ProduceException<string, string> e)
            {
                _logger.LogError($"Produce error: {e.Error.Reason}");
            }
        }
    }
    public override void Dispose()
    {
        _producer.Dispose();
        base.Dispose();
    }
}