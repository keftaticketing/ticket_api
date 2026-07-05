namespace TicketSystem.Contracts.Routes;

public sealed record CreateRouteRequest(
    Guid ToCityId,
    Guid? ToStationId = null);

public sealed record UpdateRouteRequest(
    Guid ToCityId,
    bool IsActive,
    Guid? ToStationId = null);

public sealed record RouteStationResponse(
    Guid Id,
    string Name,
    string NameAm,
    string Code,
    Guid CityId,
    string CityName,
    bool IsImplicitDefault);

public sealed record RouteResponse(
    Guid Id,
    Guid FromCityId,
    string FromCity,
    RouteStationResponse FromStation,
    Guid ToCityId,
    string ToCity,
    RouteStationResponse ToStation,
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
    RouteStationResponse FromStation,
    RouteStationResponse ToStation,
    Guid DestinationCityId,
    decimal DistanceKm,
    DateOnly Date,
    IReadOnlyList<RouteScheduleSeatMapResponse> Schedules);
