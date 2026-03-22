using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Sample.Web.Shared.Middleware;

public class RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var stopwatch = Stopwatch.StartNew();

        var urlAndMethod = $"{request.Method} {request.Path}";
        logger.LogInformation("Incoming Request: {urlAndMethod}", urlAndMethod);
        await next(context);
        stopwatch.Stop();
        logger.LogInformation("Request {urlAndMethod} completed in {ElapsedMilliseconds} ms. Outgoing Response: {StatusCode}", urlAndMethod, stopwatch.ElapsedMilliseconds, response.StatusCode);
    }
}