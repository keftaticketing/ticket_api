namespace TicketSystem.Contracts.SalesParties;

public sealed record CreateSalesPartyRequest(
    string Name,
    string Code,
    decimal AmountPerSeatEtb,
    string Source,
    string AllocationType,
    int SortOrder);

public sealed record UpdateSalesPartyRequest(
    string Name,
    decimal AmountPerSeatEtb,
    string Source,
    string AllocationType,
    int SortOrder,
    bool IsActive);

public sealed record SalesPartyResponse(
    Guid Id,
    string Name,
    string Code,
    decimal AmountPerSeatEtb,
    string Source,
    string AllocationType,
    int SortOrder,
    bool IsActive,
    DateTime CreatedAt);

public sealed record CashInventoryResponse(
    Guid SalesPartyId,
    string PartyCode,
    string PartyName,
    string Source,
    decimal BalanceEtb,
    DateTime UpdatedAt);

public sealed record CashLedgerEntryResponse(
    Guid Id,
    Guid SalesPartyId,
    string PartyCode,
    string PartyName,
    Guid TicketId,
    string EntryType,
    decimal AmountEtb,
    decimal BalanceAfterEtb,
    DateTime OccurredAt);

public sealed record TicketSaleDistributionResponse(
    Guid Id,
    Guid TicketId,
    string PartyCode,
    string PartyName,
    string Source,
    string AllocationType,
    decimal AmountEtb,
    DateTime CreatedAt);

public sealed record TicketCashBreakdownResponse(
    decimal TicketFareEtb,
    decimal SalesFeeTotalEtb,
    decimal TotalCashCollectedEtb,
    IReadOnlyList<TicketSaleDistributionResponse> Distributions);
