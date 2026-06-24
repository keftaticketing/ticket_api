namespace TicketSystem.Contracts.Schedules;

public sealed record CreateScheduleRequest(
    Guid RouteId,
    Guid BusId,
    DateTime DepartureAt,
    int SequenceNumber);

public sealed record UpdateScheduleRequest(
    DateTime DepartureAt,
    int SequenceNumber,
    string Status);

public sealed record ScheduleResponse(
    Guid Id,
    Guid RouteId,
    string FromCity,
    string ToCity,
    decimal DistanceKm,
    Guid BusId,
    string PlateNumber,
    int SeatCount,
    DateTime DepartureAt,
    int SequenceNumber,
    string Status,
    int SoldSeatCount,
    int AvailableSeatCount,
    decimal RatePerKm,
    decimal TicketPrice);

public sealed record SeatStatusResponse(
    int SeatNumber,
    string Status);

public sealed record SeatMapResponse(
    Guid ScheduleId,
    int TotalSeats,
    int SoldSeatCount,
    int AvailableSeatCount,
    bool IsFullySold,
    decimal TicketPrice,
    IReadOnlyList<SeatStatusResponse> Seats);
