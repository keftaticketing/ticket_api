namespace TicketSystem.Contracts.Cities;

public sealed record CreateCityRequest(
    string Name,
    decimal DistanceFromAddisKm);

public sealed record CityResponse(
    Guid Id,
    string Name,
    decimal DistanceFromAddisKm,
    bool IsActive,
    DateTime CreatedAt);
