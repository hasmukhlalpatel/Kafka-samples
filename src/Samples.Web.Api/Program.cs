using Samples.Web.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
.AddUserSecrets<Program>()
.AddEnvironmentVariables();

builder.ConfigureOpenTelemetry();

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

    DiagnosticsConfiguration.SampleCounter.Add(1, 
        new KeyValuePair<string, object?>("forecast.Length", forecast.Length),
        new KeyValuePair<string, object?>("forecast.Length.1", forecast.Length)
        );

    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
