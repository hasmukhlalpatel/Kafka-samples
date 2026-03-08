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
}


