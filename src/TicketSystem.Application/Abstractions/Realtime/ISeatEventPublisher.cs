namespace TicketSystem.Application.Abstractions.Realtime;

public sealed record SeatStreamEvent(string EventType, object Payload);

public interface ISeatEventPublisher
{
    IAsyncEnumerable<SeatStreamEvent> SubscribeScheduleAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<SeatStreamEvent> SubscribeRouteAsync(
        Guid routeId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    void PublishSeatSold(
        Guid scheduleId,
        Guid routeId,
        DateOnly travelDate,
        int seatNumber,
        Guid ticketId,
        int soldSeatCount,
        int availableSeatCount,
        bool isFullySold);

    void PublishScheduleUpdated(
        Guid scheduleId,
        Guid routeId,
        DateOnly travelDate,
        string status);
}
