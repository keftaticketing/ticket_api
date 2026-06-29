namespace TicketSystem.Api.Options;

/// <summary>
/// Shared header names for native / SPA clients that call the API without a browser Origin
/// (e.g. Flutter, Angular dev-server proxy).
/// </summary>
public static class ClientCredentialHeaders
{
    public const string ClientId = "X-Client-Id";
    public const string ClientKey = "X-Client-Key";
}

public abstract class ClientCredentialOptions
{
    public string ClientId { get; set; } = string.Empty;

    public string SharedKey { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(SharedKey);
}
