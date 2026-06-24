namespace TicketSystem.Api.Middleware;

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TicketSystem.Api.Options;

/// <summary>
/// Identifies native mobile clients via X-Client-Id / X-Client-Key.
/// Browser clients are validated by CORS using the Origin header instead.
/// </summary>
public sealed class MobileClientMiddleware(
    RequestDelegate next,
    IOptions<MobileClientOptions> options,
    ILogger<MobileClientMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var settings = options.Value;

        if (!settings.Enforce
            || HttpMethods.IsOptions(context.Request.Method)
            || HasBrowserOrigin(context.Request)
            || IsDocumentationPath(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (!TryValidateClient(context.Request, settings))
        {
            logger.LogWarning(
                "Rejected request without valid mobile client headers from {RemoteIp}",
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                title = "Forbidden",
                detail = "Missing or invalid mobile client credentials."
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

    private static bool TryValidateClient(HttpRequest request, MobileClientOptions settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SharedKey))
        {
            return false;
        }

        if (!request.Headers.TryGetValue(MobileClientOptions.ClientIdHeader, out var clientIdValues)
            || !request.Headers.TryGetValue(MobileClientOptions.ClientKeyHeader, out var clientKeyValues))
        {
            return false;
        }

        var clientId = clientIdValues.ToString();
        var clientKey = clientKeyValues.ToString();

        if (!string.Equals(clientId, settings.ClientId, StringComparison.Ordinal))
        {
            return false;
        }

        return FixedTimeEquals(clientKey, settings.SharedKey);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
               && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
