namespace TicketSystem.Api.Middleware;

using System.Text.Json;
using TicketSystem.Contracts.Common;
using TicketSystem.Infrastructure.Identity;

public sealed class MustChangePasswordMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> AllowedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/change-password",
        "/api/auth/logout",
        "/api/auth/refresh",
        "/api/auth/login"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var mustChange = context.User.FindFirst(JwtClaimNames.PasswordChangeRequired)?.Value;
            if (string.Equals(mustChange, bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                var path = context.Request.Path.Value ?? string.Empty;
                if (!AllowedPaths.Contains(path))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new ErrorResponse(
                        "Auth.PasswordChangeRequired",
                        "You must change your password before continuing.")));
                    return;
                }
            }
        }

        await next(context);
    }
}
