namespace TicketSystem.Api.Cors;

using TicketSystem.Api.Options;

public static class CorsPolicySetup
{
    public const string DefaultPolicyName = "Default";

    public static void AddTicketSystemCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));

        services.AddCors(options =>
        {
            options.AddPolicy(DefaultPolicyName, policy =>
            {
                policy.SetIsOriginAllowed(origin => IsAllowedOrigin(origin, configuration))
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("Content-Type", "Cache-Control");
            });
        });
    }

    private static bool IsAllowedOrigin(string origin, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return false;
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return false;
        }

        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
            ?? new CorsOptions();

        if (corsOptions.AllowedOrigins.Any(o =>
                string.Equals(o, origin, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (corsOptions.AllowLocalAdminPort4200
            && uri.Port == 4200
            && (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || uri.Host == "127.0.0.1")
            && (uri.Scheme == "http" || uri.Scheme == "https"))
        {
            return true;
        }

        return false;
    }
}
