namespace TicketSystem.Contracts.Tariffs;

public sealed record SetTariffRequest(
    Guid BusLevelId,
    Guid BusTypeId,
    decimal RatePerKm,
    Guid? RouteId = null);

public sealed record TariffRouteResponse(
    Guid Id,
    string FromCity,
    string ToCity);

public sealed record TariffBusLevelResponse(
    Guid Id,
    string Code,
    string Name,
    int Rank);

public sealed record TariffBusTypeResponse(
    Guid Id,
    string Code,
    string Name);

public sealed record TariffResponse(
    Guid Id,
    Guid? RouteId,
    TariffRouteResponse? Route,
    TariffBusLevelResponse BusLevel,
    TariffBusTypeResponse BusType,
    decimal RatePerKm,
    string Currency,
    bool IsActive,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo);
