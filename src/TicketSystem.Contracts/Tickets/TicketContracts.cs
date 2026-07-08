namespace TicketSystem.Contracts.Tickets;

using TicketSystem.Contracts.SalesParties;

public sealed record SellCashTicketRequest(
    Guid ScheduleId,
    int SeatNumber,
    string PassengerName,
    string PassengerPhone,
    string? NationalId);

public sealed record TicketResponse(
    Guid Id,
    Guid ScheduleId,
    Guid FromCityId,
    string FromCity,
    Guid FromStationId,
    string FromStationName,
    Guid ToCityId,
    string ToCity,
    Guid ToStationId,
    string ToStationName,
    Guid AssociationId,
    string AssociationName,
    Guid BusLevelId,
    string BusLevelName,
    Guid BusTypeId,
    string BusTypeName,
    Guid TariffId,
    DateTime DepartureAt,
    int SequenceNumber,
    string PlateNumber,
    string SideNumber,
    int SeatNumber,
    string PassengerName,
    string PassengerPhone,
    string? NationalId,
    decimal Price,
    decimal DistanceKm,
    decimal RatePerKm,
    string PaymentMethod,
    string SoldBy,
    DateTime SoldAt);

public sealed record SellCashTicketResponse(
    TicketResponse Ticket,
    int ScheduleSoldSeatCount,
    int ScheduleAvailableSeatCount,
    bool ScheduleIsFullySold,
    TicketCashBreakdownResponse? CashBreakdown);
