namespace TicketSystem.Contracts.SellingOptions;

using TicketSystem.Contracts.Routes;

public sealed record SellingOptionAssociationResponse(
    Guid Id,
    string Name,
    string Code);

public sealed record SellingOptionBusLevelResponse(
    Guid Id,
    string Code,
    string Name,
    int Rank);

public sealed record SellingOptionBusTypeResponse(
    Guid Id,
    string Code,
    string Name);

public sealed record SellingOptionSummaryResponse(
    string OptionKey,
    Guid RouteId,
    string FromCity,
    RouteStationResponse FromStation,
    string ToCity,
    RouteStationResponse ToStation,
    SellingOptionAssociationResponse Association,
    SellingOptionBusLevelResponse BusLevel,
    SellingOptionBusTypeResponse BusType,
    decimal DistanceKm,
    decimal RatePerKm,
    decimal TicketPrice,
    DateTime NextDepartureAt,
    int AvailableBusCount,
    int AvailableSeatCount);

public sealed record SellingOptionScheduleResponse(
    Guid ScheduleId,
    int SequenceNumber,
    DateTime DepartureAt,
    string PlateNumber,
    string SideNumber,
    int TotalSeats,
    int AvailableSeatCount,
    bool IsFullySold);
