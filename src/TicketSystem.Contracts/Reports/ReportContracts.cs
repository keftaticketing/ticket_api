namespace TicketSystem.Contracts.Reports;

public sealed record DashboardReportResponse(
    DateOnly From,
    DateOnly To,
    DashboardSummaryResponse Summary,
    IReadOnlyList<DailyTicketStatsResponse> TicketsByDay,
    IReadOnlyList<TopBusStatsResponse> TopBuses,
    IReadOnlyList<TopCounterStatsResponse> TopCounters,
    IReadOnlyList<DailyPartyRevenueResponse> RevenueByPartyByDay);

public sealed record DashboardSummaryResponse(
    int TotalTicketsSold,
    decimal TotalTicketFareEtb,
    decimal TotalSalesFeeEtb,
    decimal TotalCashCollectedEtb,
    IReadOnlyList<PartyRevenueTotalResponse> PartyTotals);

public sealed record DailyTicketStatsResponse(
    DateOnly Date,
    int TicketCount,
    decimal TicketFareEtb,
    decimal SalesFeeEtb,
    decimal TotalCashCollectedEtb);

public sealed record TopBusStatsResponse(
    Guid BusId,
    string PlateNumber,
    string SideNumber,
    int TicketsSold,
    decimal TicketFareEtb);

public sealed record TopCounterStatsResponse(
    Guid UserId,
    string UserName,
    int TicketsSold,
    decimal TicketFareEtb,
    decimal SalesFeeEtb,
    decimal TotalCashCollectedEtb);

public sealed record DailyPartyRevenueResponse(
    DateOnly Date,
    IReadOnlyList<PartyDayAmountResponse> Parties);

public sealed record PartyDayAmountResponse(
    string PartyCode,
    string PartyName,
    string Source,
    decimal AmountEtb);

public sealed record PartyRevenueTotalResponse(
    string PartyCode,
    string PartyName,
    string Source,
    decimal AmountEtb);
