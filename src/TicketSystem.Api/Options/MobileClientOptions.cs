namespace TicketSystem.Api.Options;

public sealed class MobileClientOptions : ClientCredentialOptions
{
    public const string SectionName = "MobileClient";

    /// <summary>
    /// When true, requests without a browser Origin must present valid mobile or Angular client headers.
    /// </summary>
    public bool Enforce { get; set; }

    public MobileClientOptions()
    {
        ClientId = "ticket-counter";
    }
}
