namespace TicketSystem.Api.Options;

public sealed class SseOptions
{
    public const string SectionName = "Sse";

    public int HeartbeatSeconds { get; set; } = 30;
}
