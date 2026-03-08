namespace Sample.Web.Shared.Middleware;

public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> _correlationId = new();

    public static string? CorrelationId
    {
        get => _correlationId.Value;
        set => _correlationId.Value = value;
    }

    public static void Set(string correlationId)
    {
        _correlationId.Value = correlationId;
    }

    public static void Clear()
    {
        _correlationId.Value = null;
    }
}