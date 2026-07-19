using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Observability.Shared.Extensions;

public static class OtelExtensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder, string? sourceName = null) where TBuilder : IHostApplicationBuilder
    {
        sourceName ??= builder.Environment.ApplicationName;
        ActivitySourceProvider.Instance.Initialize(sourceName);
        MeterSourceProvider.Instance.Initialize(sourceName);

        builder.ConfigureOpenTelemetry(sourceName);

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder, string? sourceName = null)
        where TBuilder : IHostApplicationBuilder
    {
        sourceName ??= builder.Environment.ApplicationName;
        Sampler sampler = new AlwaysOnSampler(); // Uncomment the following line to sample all traces (not recommended for production)
        //Sampler sampler = new TraceIdRatioBasedSampler(0.1); // Uncomment the following line to sample 10% of traces (recommended for production)

        /*
         Provides an argument for the configured sampler.

For example:

OTEL_TRACES_SAMPLER=traceidratio
OTEL_TRACES_SAMPLER_ARG=0.25
This configures a 25% sampling rate.

         */

        //sampler = new TailSamplingProcessor();
        //sampler = new ParentBasedElseAlwaysRecordSampler(new TraceIdRatioBasedSampler(0.1));
        //sampler = new ParentBasedElseAlwaysRecordSampler(new AlwaysOnSampler());

        Console.WriteLine("Using OpenTelemetry 10% sampler + ParentBasedElseAlwaysRecordSampler!, all activity might no logged.");

        var applicationInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        builder.Logging.AddOpenTelemetry(o =>
        {
            o.AddProcessor(new SimpleLogRecordExportProcessor(new DebugExporter()));
            o.IncludeFormattedMessage = true;
            o.IncludeScopes = true;
            o.ParseStateValues = true;
        });

        var otelBuilder = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(serviceName: sourceName, serviceInstanceId: Environment.MachineName);
            })
            .WithMetrics(metrics =>
            {
                metrics
                .AddMeter(sourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(sourceName)
                    .SetSampler(sampler) // 10%
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            }).WithLogging();
        //.UseAzureMonitor(o =>
        //{
        //    o.ConnectionString = applicationInsightsConnectionString;
        //   //o.Credential = new DefaultAzureCredential(); // keep null for local development, use DefaultAzureCredential for production when running in Azure
        //});

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        var useConsoleExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_CONSOLE"]);

        var applicationInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        var useApplicationInsightsExporter = !string.IsNullOrWhiteSpace(applicationInsightsConnectionString);

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        if (useApplicationInsightsExporter)
        {
            builder.Services.AddOpenTelemetry()
               .UseAzureMonitor(o =>
               {
                   o.ConnectionString = applicationInsightsConnectionString;
                   //o.Credential = new DefaultAzureCredential(); // keep null for local development, use DefaultAzureCredential for production when running in Azure
               }
               );
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }
}

class DebugExporter : BaseExporter<LogRecord>
{
    public override ExportResult Export(in Batch<LogRecord> batch)
    {
        Console.WriteLine($"Exporting {batch.Count} log records");
        foreach (var record in batch)
        {
            Console.WriteLine($"LOG: {record.FormattedMessage}");
        }
        return ExportResult.Success;
    }
}