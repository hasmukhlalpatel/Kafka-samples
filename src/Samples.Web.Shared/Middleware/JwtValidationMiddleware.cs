namespace Sample.Web.Shared.Middleware;

public class JwtValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        // In a real application, you would validate the JWT token here and set the user context accordingly.
        // Or use the built-in JWT authentication middleware provided by ASP.NET Core.
        // Best practice use APIM or API Gateway to handle authentication and pass the user context to the downstream services.

        await next(context);
    }
}
