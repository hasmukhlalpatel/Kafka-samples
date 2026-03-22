using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;

namespace Samples.Web.Shared.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder) 
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(DiagnosticsConfiguration.ServiceName);//builder.Environment.ApplicationName
            })
            .WithMetrics(matrix =>
            {
                //matrix.AddMeter(
                //    "Microsoft.AspNetCore.Hosting",
                //    "Microsoft.AspNetCore.Http",
                //    "Microsoft.AspNetCore.Routing",
                //    "Microsoft.AspNetCore.Authentication",
                //    "Microsoft.AspNetCore.Authorization",
                //    "Microsoft.AspNetCore.Server.Kestrel",
                //    "System.Net.Http",
                //    "Samples.Web.Api.Metrics");
                matrix
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation();

                matrix.AddMeter(DiagnosticsConfiguration.Meter.Name);

                matrix.AddOtlpExporter();
            })
            .WithTracing(tracing =>
            {
                tracing
                .AddSource(builder.Environment.ApplicationName)
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

        //builder.Logging.AddOpenTelemetry(options =>
        //        {
        //            options.IncludeFormattedMessage = true;
        //            options.ParseStateValues = true;
        //        });

        //builder.AddOpenTelemetryExporters();

        return builder;
    }
    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    public const string HealthEndpointPath = "/health";
    public const string AlivenessEndpointPath = "/health/live";

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

public static class DiagnosticsConfiguration
{
    public const string ServiceName = "SamplesWebApi1";
    public static Meter Meter = new Meter(ServiceName);
    public static Counter<int> SampleCounter = Meter.CreateCounter<int>("sample.counter");
}
