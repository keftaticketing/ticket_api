namespace TicketSystem.Application.Features.SellingOptions;

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Errors;
using TicketSystem.Application.Features.Auth;
using TicketSystem.Application.Features.Schedules;
using TicketSystem.Contracts.Routes;
using TicketSystem.Contracts.SellingOptions;
using TicketSystem.Domain.Common;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

public interface ISellingOptionService
{
    Task<ErrorOr<IReadOnlyList<SellingOptionSummaryResponse>>> SearchAsync(
        Guid toCityId,
        DateOnly date,
        Guid? scopedFromStationId = null,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<IReadOnlyList<SellingOptionScheduleResponse>>> GetSchedulesAsync(
        string optionKey,
        Guid? scopedFromStationId = null,
        CancellationToken cancellationToken = default);
}

public sealed class SellingOptionService(
    IApplicationDbContext db,
    IBusinessClock clock) : ISellingOptionService
{
    public async Task<ErrorOr<IReadOnlyList<SellingOptionSummaryResponse>>> SearchAsync(
        Guid toCityId,
        DateOnly date,
        Guid? scopedFromStationId = null,
        CancellationToken cancellationToken = default)
    {
        var routeResult = await ResolveRouteAsync(toCityId, scopedFromStationId, cancellationToken);
        if (routeResult.IsError)
        {
            return routeResult.Errors;
        }

        var route = routeResult.Value;
        var schedulesResult = await LoadSchedulesAsync(route.Id, date, cancellationToken);
        if (schedulesResult.IsError)
        {
            return schedulesResult.Errors;
        }

        var schedules = schedulesResult.Value;
        if (schedules.Count == 0)
        {
            return Array.Empty<SellingOptionSummaryResponse>();
        }

        var soldCounts = await LoadSoldSeatCountsAsync(schedules, cancellationToken);
        var summaries = new List<SellingOptionSummaryResponse>();

        foreach (var group in schedules
                     .GroupBy(x => new { x.AssociationId, x.BusLevelId, x.BusTypeId })
                     .OrderBy(x => x.First().BusLevel.Rank)
                     .ThenBy(x => x.First().BusType.Code))
        {
            var sample = group.First();
            var ratePerKm = sample.ResolvedRatePerKm;
            var ticketPrice = sample.ResolvedTicketPrice;
            var availability = group
                .Select(schedule => BuildAvailability(schedule, soldCounts))
                .Where(x => x.AvailableSeatCount > 0)
                .ToList();

            if (availability.Count == 0)
            {
                continue;
            }

            summaries.Add(new SellingOptionSummaryResponse(
                SellingOptionKey.Build(route.Id, sample.AssociationId, sample.BusLevelId, sample.BusTypeId, date),
                route.Id,
                route.FromCity.Name,
                MapStation(route.FromStation),
                route.ToCity.Name,
                MapStation(route.ToStation),
                new SellingOptionAssociationResponse(sample.Association.Id, sample.Association.Name, sample.Association.Code),
                new SellingOptionBusLevelResponse(
                    sample.BusLevel.Id,
                    sample.BusLevel.Code,
                    sample.BusLevel.Name,
                    sample.BusLevel.Rank),
                new SellingOptionBusTypeResponse(sample.BusType.Id, sample.BusType.Code, sample.BusType.Name),
                sample.ResolvedDistanceKm,
                ratePerKm,
                ticketPrice,
                clock.ToLocalDateTime(availability.Min(x => x.Schedule.DepartureAt)),
                availability.Count,
                availability.Sum(x => x.AvailableSeatCount)));
        }

        return summaries;
    }

    public async Task<ErrorOr<IReadOnlyList<SellingOptionScheduleResponse>>> GetSchedulesAsync(
        string optionKey,
        Guid? scopedFromStationId = null,
        CancellationToken cancellationToken = default)
    {
        var parsed = SellingOptionKey.TryParse(optionKey);
        if (parsed.IsError)
        {
            return parsed.Errors;
        }

        var (routeId, associationId, busLevelId, busTypeId, date) = parsed.Value;
        var route = await db.Routes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == routeId && x.IsActive, cancellationToken);
        if (route is null)
        {
            return DomainErrors.RouteNotFound;
        }

        var scopeResult = TicketerSellingScope.EnsureRouteOriginMatches(route.FromStationId, scopedFromStationId);
        if (scopeResult.IsError)
        {
            return scopeResult.Errors;
        }

        var schedulesResult = await LoadSchedulesAsync(routeId, date, cancellationToken);
        if (schedulesResult.IsError)
        {
            return schedulesResult.Errors;
        }

        var schedules = schedulesResult.Value
            .Where(x => x.AssociationId == associationId
                        && x.BusLevelId == busLevelId
                        && x.BusTypeId == busTypeId)
            .OrderBy(x => x.SequenceNumber)
            .ThenBy(x => x.DepartureAt)
            .ToList();

        if (schedules.Count == 0)
        {
            return Array.Empty<SellingOptionScheduleResponse>();
        }

        var soldCounts = await LoadSoldSeatCountsAsync(schedules, cancellationToken);
        return schedules
            .Select(schedule =>
            {
                var availability = BuildAvailability(schedule, soldCounts);
                return new SellingOptionScheduleResponse(
                    schedule.Id,
                    schedule.SequenceNumber,
                    clock.ToLocalDateTime(schedule.DepartureAt),
                    schedule.Bus.PlateNumber,
                    schedule.Bus.SideNumber,
                    schedule.Bus.SeatCount,
                    availability.AvailableSeatCount,
                    availability.AvailableSeatCount == 0);
            })
            .ToList();
    }

    private async Task<ErrorOr<Route>> ResolveRouteAsync(
        Guid destinationCityId,
        Guid? fromStationId,
        CancellationToken cancellationToken)
    {
        var addisAbaba = await db.Cities.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Name == CityNames.AddisAbaba, cancellationToken);
        if (addisAbaba is null)
        {
            return DomainErrors.AddisAbabaNotFound;
        }

        if (destinationCityId == addisAbaba.Id)
        {
            return DomainErrors.DestinationCityRequired;
        }

        var routeQuery = db.Routes.AsNoTracking()
            .Include(x => x.FromCity)
            .Include(x => x.FromStation).ThenInclude(x => x.City)
            .Include(x => x.ToCity)
            .Include(x => x.ToStation).ThenInclude(x => x.City)
            .Where(x => x.IsActive
                        && x.FromCityId == addisAbaba.Id
                        && x.ToCityId == destinationCityId);

        if (fromStationId.HasValue)
        {
            routeQuery = routeQuery.Where(x => x.FromStationId == fromStationId.Value);
        }

        var route = await routeQuery
            .OrderBy(x => x.FromStation.IsImplicitDefault ? 0 : 1)
            .ThenBy(x => x.FromStation.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return route is null ? DomainErrors.RouteNotFound : route;
    }

    private async Task<ErrorOr<List<Schedule>>> LoadSchedulesAsync(
        Guid routeId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var schedules = await db.Schedules.AsNoTracking()
            .Include(x => x.Bus)
            .Include(x => x.Association)
            .Include(x => x.BusLevel)
            .Include(x => x.BusType)
            .Where(x => x.RouteId == routeId
                        && x.DepartureDate == date
                        && (x.Status == ScheduleStatus.Scheduled || x.Status == ScheduleStatus.Boarding))
            .OrderForOptionDisplay()
            .ToListAsync(cancellationToken);

        return schedules;
    }

    private async Task<Dictionary<Guid, int>> LoadSoldSeatCountsAsync(
        IReadOnlyList<Schedule> schedules,
        CancellationToken cancellationToken)
    {
        var scheduleIds = schedules.Select(x => x.Id).ToList();
        return await db.Tickets.AsNoTracking()
            .Where(x => scheduleIds.Contains(x.ScheduleId))
            .GroupBy(x => x.ScheduleId)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
    }

    private static (Schedule Schedule, int AvailableSeatCount) BuildAvailability(
        Schedule schedule,
        IReadOnlyDictionary<Guid, int> soldCounts)
    {
        var soldCount = soldCounts.GetValueOrDefault(schedule.Id);
        return (schedule, schedule.Bus.SeatCount - soldCount);
    }

    private static RouteStationResponse MapStation(Station station) =>
        new(
            station.Id,
            station.Name,
            station.NameAm,
            station.Code,
            station.CityId,
            station.City.Name,
            station.IsImplicitDefault);
}
