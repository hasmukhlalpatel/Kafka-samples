using Sample.Web.Shared.Middleware;

namespace Samples.Web.Shared.Extensions;

public static class WebApplicationExtensions
{
    public static T UseObservability<T>(this T app)
        where T : IApplicationBuilder
    {
        app.UseMiddleware<LoggingMiddleware>();
        return app;
    }
    public static T UseCustomExceptionHandling<T>(this T app)
        where T : IApplicationBuilder
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        return app;
    }
    public static T UseCustomRequestResponseLogging<T>(this T app)
        where T : IApplicationBuilder
    {
        app.UseMiddleware<RequestResponseLoggingMiddleware>();
        return app;
    }

    public static WebApplication UseSwaggerAndOpenApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        return app;
    }

    private const string ApiVersion = "v1";
    private const string HealthEndpoint = "/health";
    private const string livenessEndpoint = "/health/live";
    private const string readinessEndpoint = "/health/ready";

    public static WebApplication MapDefaultHealthEndpoints(this WebApplication app)
    { 
        app.MapHealthChecks(HealthEndpoint);
        app.MapHealthChecks(livenessEndpoint, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            //Predicate = r => r.Tags.Contains("live"),
            Predicate = _ => true,
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        exception = e.Value.Exception?.Message,
                        duration = e.Value.Duration.ToString()
                    })
                });
                await context.Response.WriteAsync(result);
            }
        });

        return app;
    }
}


