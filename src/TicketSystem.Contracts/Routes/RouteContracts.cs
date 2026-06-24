namespace TicketSystem.Contracts.Routes;

public sealed record CreateRouteRequest(Guid ToCityId);

public sealed record UpdateRouteRequest(
    Guid ToCityId,
    bool IsActive);

public sealed record RouteResponse(
    Guid Id,
    Guid FromCityId,
    string FromCity,
    Guid ToCityId,
    string ToCity,
    decimal DistanceKm,
    bool IsActive,
    DateTime CreatedAt);

public sealed record RouteScheduleSeatMapResponse(
    Guid ScheduleId,
    Guid BusId,
    string PlateNumber,
    string SideNumber,
    DateTime DepartureAt,
    int SequenceNumber,
    int TotalSeats,
    int SoldSeatCount,
    int AvailableSeatCount,
    bool IsFullySold,
    decimal TicketPrice,
    IReadOnlyList<Schedules.SeatStatusResponse> Seats);

public sealed record RouteSeatMapsResponse(
    Guid RouteId,
    string FromCity,
    string ToCity,
    Guid DestinationCityId,
    decimal DistanceKm,
    DateOnly Date,
    IReadOnlyList<RouteScheduleSeatMapResponse> Schedules);
