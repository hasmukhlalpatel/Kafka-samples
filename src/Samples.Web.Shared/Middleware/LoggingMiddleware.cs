using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Sample.Web.Shared.Middleware;

public class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var xHeaders = context.Request.Headers
            .Where(h => h.Key.StartsWith("x-", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        const string correlationHeader = "x-correlation-id";

        if (!xHeaders.TryGetValue(correlationHeader, out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            xHeaders[correlationHeader] = correlationId;

            context.Request.Headers[correlationHeader] = correlationId;
        }
        CorrelationContext.Set(correlationId);
        context.Items[correlationHeader] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[correlationHeader] = correlationId;
            return Task.CompletedTask;
        });

        try
        {
            using var _ = logger.BeginScope(xHeaders);
            logger.LogInformation("Handling request: {Method} {Path}", context.Request.Method, context.Request.Path);
            await next(context);
        }
        finally
        {
            CorrelationContext.Clear();
        }
    }
}
