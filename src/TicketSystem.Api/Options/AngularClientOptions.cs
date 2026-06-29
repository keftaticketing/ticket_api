namespace TicketSystem.Api.Options;

public sealed class AngularClientOptions : ClientCredentialOptions
{
    public const string SectionName = "AngularClient";

    public AngularClientOptions()
    {
        ClientId = "ticket-admin";
    }
}
