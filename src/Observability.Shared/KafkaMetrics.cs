using System.Diagnostics.Metrics;

namespace Observability.Shared;

public static class KafkaMetrics
{
    //public static readonly Meter Meter = new("MyCompany.KafkaConsumer", "1.0");
    public static readonly Meter Meter = MeterSourceProvider.Instance.Meter;

    public static readonly Counter<long> MessagesConsumed =
        Meter.CreateCounter<long>(
            "kafka.messages.consumed",
            unit: "{message}",
            description: "Total Kafka messages consumed");

    public static readonly Histogram<double> ProcessingDuration =
        Meter.CreateHistogram<double>(
            "kafka.message.processing.duration",
            unit: "ms",
            description: "Kafka message processing duration");
}