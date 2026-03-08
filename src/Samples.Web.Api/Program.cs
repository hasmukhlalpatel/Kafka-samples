using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Samples.Web.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
.AddUserSecrets<Program>()
.AddEnvironmentVariables();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService(builder.Environment.ApplicationName);
    })
    .WithMetrics(matrix =>
    {
        matrix.AddMeter(
            "Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Http",
            "Microsoft.AspNetCore.Routing",
            "Microsoft.AspNetCore.Authentication",
            "Microsoft.AspNetCore.Authorization",
            "Microsoft.AspNetCore.Server.Kestrel",
            "System.Net.Http",
            "Samples.Web.Api.Metrics");
        matrix
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation();

        matrix.AddOtlpExporter();
    })
    .WithTracing(tracing=>
    {
        tracing
        .AddAspNetCoreInstrumentation(o =>
        {
            o.EnrichWithHttpRequest = (activity, httpRequest) =>
            {
                activity.SetTag("requestProtocol", httpRequest.Protocol);
            };
            o.EnrichWithHttpResponse = (activity, httpResponse) =>
            {
                activity.SetTag("responseLength", httpResponse.ContentLength);
                // Access request object if needed
                // response.HttpContext.Request
                activity.DisplayName = "CustomDisplayName";
                // Overrides the value
                activity.SetTag("http.route", "CustomRoute");
                // Removes the tag
                activity.SetTag("network.protocol.version", null);
            };
            o.EnrichWithException = (activity, exception) =>
            {
                if (exception.Source != null)
                {
                    activity.SetTag("exception.source", exception.Source);
                }
            };
        })
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation();

        tracing.AddOtlpExporter();
    });

builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeFormattedMessage = true;
    options.ParseStateValues = true;
});

builder.Services
    //.AddOTELObservability()
    .AddSwaggerAndOpenApi()
    .AddDefaultHealthChecks();

var app = builder.Build();

app
    .UseObservability()
    .UseCustomRequestResponseLogging()
    .UseCustomExceptionHandling();

app
    .UseSwaggerAndOpenApi()
    .MapDefaultHealthEndpoints();

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (ILogger<Program> logger) =>
{
    logger.LogInformation("Generating weather forecast");
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    logger.LogWarning("Weather forecast generated with {Count} entries" , forecast.Length);
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
