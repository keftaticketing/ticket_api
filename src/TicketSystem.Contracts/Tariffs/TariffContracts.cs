namespace TicketSystem.Contracts.Tariffs;

public sealed record SetTariffRequest(decimal RatePerKm);

public sealed record TariffResponse(
    Guid Id,
    decimal RatePerKm,
    string Currency,
    bool IsActive,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo);
