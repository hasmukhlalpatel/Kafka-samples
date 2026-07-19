using System.Diagnostics.Metrics;

namespace Observability.Shared.Extensions;

public static class CounterExtensions
{
    public static void Add<T>(this Counter<T> counter, T value, string key, object? val) where T : struct
    {
        counter.Add(value, [new(key, val)]);
    }
}
