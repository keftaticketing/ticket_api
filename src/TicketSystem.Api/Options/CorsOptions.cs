namespace TicketSystem.Api.Options;

public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Allow http/https on localhost and 127.0.0.1 port 4200 (Angular dev server).
    /// </summary>
    public bool AllowLocalAdminPort4200 { get; set; } = true;

    /// <summary>
    /// Extra allowed browser origins, e.g. https://admin.example.com
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];
}
