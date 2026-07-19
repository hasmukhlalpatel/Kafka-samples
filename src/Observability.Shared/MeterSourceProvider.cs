using System.Diagnostics.Metrics;

namespace Observability.Shared;

public sealed class MeterSourceProvider
{
    private MeterSourceProvider() { }
    public void Initialize(string meterSourceName)
    {
        if (string.IsNullOrWhiteSpace(meterSourceName))
            throw new ArgumentException("Meter source name cannot be null or whitespace.", nameof(meterSourceName));

        if (Meter != null)
            throw new InvalidOperationException("MeterSourceProvider has already been initialized.");
        Meter = new Meter(meterSourceName);
    }
    public static MeterSourceProvider Instance { get; private set; } = new MeterSourceProvider();
    public Meter Meter { get; private set; }

    public Counter<T> CreateCounter<T>(string name, string? unit = null, string? description = null)
        where T : struct
    {
        return Meter.CreateCounter<T>(name, unit, description);
    }
    public Histogram<T> CreateHistogram<T>(string name, string? unit = null, string? description = null)
        where T : struct
    {
        return Meter.CreateHistogram<T>(name, unit, description);
    }
}


