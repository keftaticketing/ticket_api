namespace TicketSystem.Api.Options;

public sealed class MobileClientOptions
{
    public const string SectionName = "MobileClient";

    public const string ClientIdHeader = "X-Client-Id";
    public const string ClientKeyHeader = "X-Client-Key";

    /// <summary>
    /// Expected client id for the ticket counter mobile app.
    /// </summary>
    public string ClientId { get; set; } = "ticket-counter";

    /// <summary>
    /// Shared secret sent by the mobile app on every request (no browser Origin).
    /// </summary>
    public string SharedKey { get; set; } = string.Empty;

    /// <summary>
    /// When true, requests without a browser Origin must present valid client headers.
    /// </summary>
    public bool Enforce { get; set; }
}
