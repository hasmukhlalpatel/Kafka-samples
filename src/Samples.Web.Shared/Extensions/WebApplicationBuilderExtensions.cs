using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics.Metrics;

namespace Samples.Web.Shared.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureOpenTelemetry(this WebApplicationBuilder builder)
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
        return builder;
    }
}

public static class DiagnosticsConfiguration
{
    public const string ServiceName = "SamplesWebApi1";
    public static Meter Meter = new Meter(ServiceName);
    public static Counter<int> SampleCounter = Meter.CreateCounter<int>("sample.counter");
}
