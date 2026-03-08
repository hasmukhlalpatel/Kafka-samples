namespace Sample.Web.Shared.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = CorrelationContext.CorrelationId;

        using var _ = logger.BeginScope(new Dictionary<string, object>
        {
            ["correlationId"] = correlationId,
            ["path"] = context.Request.Path
        });

        logger.LogError(exception, "Unhandled exception occurred.");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var error = new
        {
            message = "An unexpected error occurred.",
            correlationId
        };

        await context.Response.WriteAsJsonAsync(error);
    }
}
