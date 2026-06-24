namespace TicketSystem.Application.Features.Reports;

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Common;
using TicketSystem.Application.Errors;
using TicketSystem.Contracts.Reports;
using TicketSystem.Domain.Enums;

public interface IReportsService
{
    Task<ErrorOr<DashboardReportResponse>> GetDashboardAsync(
        DateOnly? from,
        DateOnly? to,
        int topLimit,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<IReadOnlyList<DailyTicketStatsResponse>>> GetTicketsByDayAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<IReadOnlyList<TopBusStatsResponse>>> GetTopBusesAsync(
        DateOnly? from,
        DateOnly? to,
        int topLimit,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<IReadOnlyList<TopCounterStatsResponse>>> GetTopCountersAsync(
        DateOnly? from,
        DateOnly? to,
        int topLimit,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<IReadOnlyList<DailyPartyRevenueResponse>>> GetRevenueByPartyAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default);
}

public sealed class ReportsService(IApplicationDbContext db, IBusinessClock clock) : IReportsService
{
    private const int DefaultTopLimit = 10;
    private const int MaxTopLimit = 50;
    private const int MaxRangeDays = 366;
    private const int DefaultRangeDays = 30;

    public async Task<ErrorOr<DashboardReportResponse>> GetDashboardAsync(
        DateOnly? from,
        DateOnly? to,
        int topLimit,
        CancellationToken cancellationToken = default)
    {
        var rangeResult = ResolveRange(from, to);
        if (rangeResult.IsError)
        {
            return rangeResult.Errors;
        }

        var (fromDate, toDate, startUtc, endUtc) = rangeResult.Value;
        var limit = NormalizeTopLimit(topLimit);

        var tickets = await LoadTicketsAsync(startUtc, endUtc, cancellationToken);
        var distributions = await LoadDistributionsAsync(startUtc, endUtc, cancellationToken);

        return new DashboardReportResponse(
            fromDate,
            toDate,
            BuildSummary(tickets, distributions),
            BuildTicketsByDay(tickets, distributions),
            await BuildTopBusesAsync(startUtc, endUtc, limit, cancellationToken),
            BuildTopCounters(tickets, distributions, limit),
            BuildRevenueByPartyByDay(distributions));
    }

    public async Task<ErrorOr<IReadOnlyList<DailyTicketStatsResponse>>> GetTicketsByDayAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var rangeResult = ResolveRange(from, to);
        if (rangeResult.IsError)
        {
            return rangeResult.Errors;
        }

        var (_, _, startUtc, endUtc) = rangeResult.Value;
        var tickets = await LoadTicketsAsync(startUtc, endUtc, cancellationToken);
        var distributions = await LoadDistributionsAsync(startUtc, endUtc, cancellationToken);

        return BuildTicketsByDay(tickets, distributions);
    }

    public async Task<ErrorOr<IReadOnlyList<TopBusStatsResponse>>> GetTopBusesAsync(
        DateOnly? from,
        DateOnly? to,
        int topLimit,
        CancellationToken cancellationToken = default)
    {
        var rangeResult = ResolveRange(from, to);
        if (rangeResult.IsError)
        {
            return rangeResult.Errors;
        }

        var (_, _, startUtc, endUtc) = rangeResult.Value;
        return await BuildTopBusesAsync(startUtc, endUtc, NormalizeTopLimit(topLimit), cancellationToken);
    }

    public async Task<ErrorOr<IReadOnlyList<TopCounterStatsResponse>>> GetTopCountersAsync(
        DateOnly? from,
        DateOnly? to,
        int topLimit,
        CancellationToken cancellationToken = default)
    {
        var rangeResult = ResolveRange(from, to);
        if (rangeResult.IsError)
        {
            return rangeResult.Errors;
        }

        var (_, _, startUtc, endUtc) = rangeResult.Value;
        var tickets = await LoadTicketsAsync(startUtc, endUtc, cancellationToken);
        var distributions = await LoadDistributionsAsync(startUtc, endUtc, cancellationToken);

        return BuildTopCounters(tickets, distributions, NormalizeTopLimit(topLimit));
    }

    public async Task<ErrorOr<IReadOnlyList<DailyPartyRevenueResponse>>> GetRevenueByPartyAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var rangeResult = ResolveRange(from, to);
        if (rangeResult.IsError)
        {
            return rangeResult.Errors;
        }

        var (_, _, startUtc, endUtc) = rangeResult.Value;
        var distributions = await LoadDistributionsAsync(startUtc, endUtc, cancellationToken);

        return BuildRevenueByPartyByDay(distributions);
    }

    private ErrorOr<(DateOnly From, DateOnly To, DateTime StartUtc, DateTime EndUtc)> ResolveRange(
        DateOnly? from,
        DateOnly? to)
    {
        var toDate = to ?? clock.Today;
        var fromDate = from ?? toDate.AddDays(-(DefaultRangeDays - 1));

        if (fromDate > toDate)
        {
            return DomainErrors.InvalidReportDateRange;
        }

        if (toDate.DayNumber - fromDate.DayNumber >= MaxRangeDays)
        {
            return DomainErrors.ReportDateRangeTooLarge;
        }

        var (startUtc, _) = clock.GetUtcRangeForLocalDate(fromDate);
        var (_, endUtc) = clock.GetUtcRangeForLocalDate(toDate);

        return (fromDate, toDate, startUtc, endUtc);
    }

    private static int NormalizeTopLimit(int topLimit) =>
        topLimit <= 0 ? DefaultTopLimit : Math.Min(topLimit, MaxTopLimit);

    private async Task<List<TicketRow>> LoadTicketsAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken) =>
        await db.Tickets.AsNoTracking()
            .Where(t => t.SoldAt >= startUtc && t.SoldAt < endUtc)
            .Select(t => new TicketRow(
                t.Id,
                t.SoldAt,
                t.Price,
                t.SoldByUserId,
                t.SoldByUserName))
            .ToListAsync(cancellationToken);

    private async Task<List<DistributionRow>> LoadDistributionsAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken) =>
        await (
            from d in db.TicketSaleDistributions.AsNoTracking()
            join t in db.Tickets.AsNoTracking() on d.TicketId equals t.Id
            where t.SoldAt >= startUtc && t.SoldAt < endUtc
            select new DistributionRow(
                d.TicketId,
                d.PartyCode,
                d.PartyName,
                d.Source,
                d.AmountEtb,
                t.SoldAt))
            .ToListAsync(cancellationToken);

    private async Task<List<TopBusStatsResponse>> BuildTopBusesAsync(
        DateTime startUtc,
        DateTime endUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from t in db.Tickets.AsNoTracking()
            join s in db.Schedules.AsNoTracking() on t.ScheduleId equals s.Id
            join b in db.Buses.AsNoTracking() on s.BusId equals b.Id
            where t.SoldAt >= startUtc && t.SoldAt < endUtc
            group t by new { b.Id, b.PlateNumber, b.SideNumber } into g
            orderby g.Count() descending, g.Sum(x => x.Price) descending
            select new
            {
                g.Key.Id,
                g.Key.PlateNumber,
                g.Key.SideNumber,
                TicketsSold = g.Count(),
                TicketFareEtb = g.Sum(x => x.Price)
            })
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new TopBusStatsResponse(
            x.Id,
            x.PlateNumber,
            x.SideNumber,
            x.TicketsSold,
            Money.Round(x.TicketFareEtb))).ToList();
    }

    private List<DailyTicketStatsResponse> BuildTicketsByDay(
        IReadOnlyList<TicketRow> tickets,
        IReadOnlyList<DistributionRow> distributions)
    {
        var salesFeeByTicket = distributions
            .Where(x => x.Source == SalesPartySource.SalesFee)
            .GroupBy(x => x.TicketId)
            .ToDictionary(g => g.Key, g => Money.Round(g.Sum(x => x.AmountEtb)));

        return tickets
            .GroupBy(x => clock.ToLocalDate(x.SoldAt))
            .OrderBy(g => g.Key)
            .Select(dayGroup =>
            {
                var fare = Money.Round(dayGroup.Sum(x => x.Price));
                var salesFee = Money.Round(dayGroup.Sum(x =>
                    salesFeeByTicket.GetValueOrDefault(x.Id)));
                return new DailyTicketStatsResponse(
                    dayGroup.Key,
                    dayGroup.Count(),
                    fare,
                    salesFee,
                    fare);
            })
            .ToList();
    }

    private List<TopCounterStatsResponse> BuildTopCounters(
        IReadOnlyList<TicketRow> tickets,
        IReadOnlyList<DistributionRow> distributions,
        int limit)
    {
        var salesFeeByTicket = distributions
            .Where(x => x.Source == SalesPartySource.SalesFee)
            .GroupBy(x => x.TicketId)
            .ToDictionary(g => g.Key, g => Money.Round(g.Sum(x => x.AmountEtb)));

        return tickets
            .GroupBy(x => new { x.SoldByUserId, x.SoldByUserName })
            .Select(g =>
            {
                var fare = Money.Round(g.Sum(x => x.Price));
                var salesFee = Money.Round(g.Sum(x =>
                    salesFeeByTicket.GetValueOrDefault(x.Id)));
                return new TopCounterStatsResponse(
                    g.Key.SoldByUserId,
                    g.Key.SoldByUserName,
                    g.Count(),
                    fare,
                    salesFee,
                    fare);
            })
            .OrderByDescending(x => x.TicketsSold)
            .ThenByDescending(x => x.TotalCashCollectedEtb)
            .Take(limit)
            .ToList();
    }

    private List<DailyPartyRevenueResponse> BuildRevenueByPartyByDay(
        IReadOnlyList<DistributionRow> distributions) =>
        distributions
            .GroupBy(x => clock.ToLocalDate(x.SoldAt))
            .OrderBy(g => g.Key)
            .Select(dayGroup => new DailyPartyRevenueResponse(
                dayGroup.Key,
                dayGroup
                    .GroupBy(x => new { x.PartyCode, x.PartyName, x.Source })
                    .Select(pg => new PartyDayAmountResponse(
                        pg.Key.PartyCode,
                        pg.Key.PartyName,
                        pg.Key.Source.ToString(),
                        Money.Round(pg.Sum(x => x.AmountEtb))))
                    .OrderBy(x => x.PartyCode)
                    .ToList()))
            .ToList();

    private DashboardSummaryResponse BuildSummary(
        IReadOnlyList<TicketRow> tickets,
        IReadOnlyList<DistributionRow> distributions)
    {
        var totalFare = Money.Round(tickets.Sum(x => x.Price));
        var salesFee = Money.Round(distributions
            .Where(x => x.Source == SalesPartySource.SalesFee)
            .Sum(x => x.AmountEtb));

        var partyTotals = distributions
            .GroupBy(x => new { x.PartyCode, x.PartyName, x.Source })
            .Select(g => new PartyRevenueTotalResponse(
                g.Key.PartyCode,
                g.Key.PartyName,
                g.Key.Source.ToString(),
                Money.Round(g.Sum(x => x.AmountEtb))))
            .OrderBy(x => x.PartyCode)
            .ToList();

        return new DashboardSummaryResponse(
            tickets.Count,
            totalFare,
            salesFee,
            totalFare,
            partyTotals);
    }

    private sealed record TicketRow(
        Guid Id,
        DateTime SoldAt,
        decimal Price,
        Guid SoldByUserId,
        string SoldByUserName);

    private sealed record DistributionRow(
        Guid TicketId,
        string PartyCode,
        string PartyName,
        SalesPartySource Source,
        decimal AmountEtb,
        DateTime SoldAt);
}
