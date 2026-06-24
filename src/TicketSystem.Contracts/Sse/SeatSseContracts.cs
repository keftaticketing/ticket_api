namespace TicketSystem.Contracts.Sse;

public static class SeatSseEventNames
{
    public const string Connected = "connected";
    public const string SeatHeld = "seat.held";
    public const string SeatReleased = "seat.released";
    public const string SeatSold = "seat.sold";
    public const string ScheduleUpdated = "schedule.updated";
    public const string PaymentConfirmed = "payment.confirmed";
    public const string Heartbeat = "heartbeat";
}

public sealed record SseConnectedPayload(Guid ScheduleId, DateTime ServerTime);

public sealed record SseRouteConnectedPayload(Guid RouteId, DateOnly Date, DateTime ServerTime);

public sealed record SseHeartbeatPayload(DateTime ServerTime);

public sealed record SseSeatSoldPayload(
    Guid ScheduleId,
    int SeatNumber,
    Guid TicketId,
    int SoldSeatCount,
    int AvailableSeatCount,
    bool IsFullySold);

public sealed record SseScheduleUpdatedPayload(Guid ScheduleId, string Status);
