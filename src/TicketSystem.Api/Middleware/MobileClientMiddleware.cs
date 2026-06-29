namespace TicketSystem.Api.Middleware;

using Microsoft.Extensions.Options;
using TicketSystem.Api.Options;
using TicketSystem.Api.Security;

/// <summary>
/// Identifies non-browser clients (Flutter, Angular via dev proxy) via X-Client-Id / X-Client-Key.
/// Browser clients with an allowed CORS Origin skip this check.
/// </summary>
public sealed class MobileClientMiddleware(
    RequestDelegate next,
    IOptions<MobileClientOptions> mobileOptions,
    IOptions<AngularClientOptions> angularOptions,
    ILogger<MobileClientMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var mobile = mobileOptions.Value;
        var angular = angularOptions.Value;

        if (!mobile.Enforce
            || HttpMethods.IsOptions(context.Request.Method)
            || HasBrowserOrigin(context.Request)
            || IsDocumentationPath(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (!ClientCredentialValidator.TryValidate(context.Request, mobile, angular))
        {
            logger.LogWarning(
                "Rejected request without valid client credentials from {RemoteIp}",
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                code = "Client.InvalidCredentials",
                description = "Missing or invalid client credentials."
            });
            return;
        }

        await next(context);
    }

    private static bool HasBrowserOrigin(HttpRequest request) =>
        request.Headers.ContainsKey("Origin");

    private static bool IsDocumentationPath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase)
               || value.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase);
    }
}
