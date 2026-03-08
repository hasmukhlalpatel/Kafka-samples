using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Samples.Web.Shared.Extensions;

public static class ServcieCollectionExtensions
{
    public static IServiceCollection AddOTELObservability(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithTracing(builder => builder
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
            }));

        return services;
    }
    public static IServiceCollection AddRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions
                .AddFixedWindowLimiter(policyName: "fixed", options =>
                {
                    options.PermitLimit = 100;
                    options.Window = TimeSpan.FromSeconds(60);
                });
        });
        return services;
    }
}


