namespace TicketSystem.Application.Features.Routes;

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Features.Schedules;
using TicketSystem.Application.Errors;
using TicketSystem.Contracts.Routes;
using TicketSystem.Contracts.Schedules;
using TicketSystem.Domain.Common;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

public interface IRouteService
{
    Task<ErrorOr<RouteResponse>> CreateAsync(CreateRouteRequest request, CancellationToken cancellationToken = default);
    Task<ErrorOr<IReadOnlyList<RouteResponse>>> GetAllAsync(Guid? toCityId, CancellationToken cancellationToken = default);
    Task<ErrorOr<RouteResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ErrorOr<RouteResponse>> UpdateAsync(Guid id, UpdateRouteRequest request, CancellationToken cancellationToken = default);
    Task<ErrorOr<RouteSeatMapsResponse>> GetSeatMapsByDestinationAsync(Guid destinationCityId, DateOnly date, CancellationToken cancellationToken = default);
}

public sealed class RouteService(IApplicationDbContext db, IBusinessClock clock) : IRouteService
{
    public async Task<ErrorOr<RouteResponse>> CreateAsync(CreateRouteRequest request, CancellationToken cancellationToken = default)
    {
        var addisAbaba = await GetAddisAbabaAsync(cancellationToken);
        if (addisAbaba is null)
        {
            return DomainErrors.AddisAbabaNotFound;
        }

        if (request.ToCityId == addisAbaba.Id)
        {
            return DomainErrors.SameOriginDestination;
        }

        var toCity = await db.Cities.SingleOrDefaultAsync(x => x.Id == request.ToCityId && x.IsActive, cancellationToken);
        if (toCity is null)
        {
            return DomainErrors.CityInactive;
        }

        if (toCity.Name == CityNames.AddisAbaba)
        {
            return DomainErrors.SameOriginDestination;
        }

        if (await db.Routes.AnyAsync(x => x.FromCityId == addisAbaba.Id && x.ToCityId == request.ToCityId, cancellationToken))
        {
            return DomainErrors.DuplicateRoute;
        }

        var route = new Route
        {
            Id = Guid.NewGuid(),
            FromCityId = addisAbaba.Id,
            ToCityId = toCity.Id,
            DistanceKm = toCity.DistanceFromAddisKm
        };

        db.Routes.Add(route);
        await db.SaveChangesAsync(cancellationToken);
        return await MapByIdAsync(route.Id, cancellationToken);
    }

    public async Task<ErrorOr<IReadOnlyList<RouteResponse>>> GetAllAsync(
        Guid? toCityId,
        CancellationToken cancellationToken = default)
    {
        var addisAbaba = await GetAddisAbabaAsync(cancellationToken);
        if (addisAbaba is null)
        {
            return DomainErrors.AddisAbabaNotFound;
        }

        var query = db.Routes.AsNoTracking()
            .Include(x => x.FromCity)
            .Include(x => x.ToCity)
            .Where(x => x.IsActive && x.FromCityId == addisAbaba.Id);

        if (toCityId.HasValue)
        {
            query = query.Where(x => x.ToCityId == toCityId.Value);
        }

        var routes = await query
            .OrderBy(x => x.ToCity.DistanceFromAddisKm)
            .ThenBy(x => x.ToCity.Name)
            .ToListAsync(cancellationToken);

        return routes.Select(Map).ToList();
    }

    public async Task<ErrorOr<RouteResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await MapByIdAsync(id, cancellationToken);
    }

    public async Task<ErrorOr<RouteResponse>> UpdateAsync(Guid id, UpdateRouteRequest request, CancellationToken cancellationToken = default)
    {
        var addisAbaba = await GetAddisAbabaAsync(cancellationToken);
        if (addisAbaba is null)
        {
            return DomainErrors.AddisAbabaNotFound;
        }

        var route = await db.Routes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (route is null)
        {
            return DomainErrors.RouteNotFound;
        }

        if (request.ToCityId == addisAbaba.Id)
        {
            return DomainErrors.SameOriginDestination;
        }

        var toCity = await db.Cities.SingleOrDefaultAsync(x => x.Id == request.ToCityId && x.IsActive, cancellationToken);
        if (toCity is null)
        {
            return DomainErrors.CityInactive;
        }

        if (await db.Routes.AnyAsync(
                x => x.Id != id
                     && x.FromCityId == addisAbaba.Id
                     && x.ToCityId == request.ToCityId,
                cancellationToken))
        {
            return DomainErrors.DuplicateRoute;
        }

        route.FromCityId = addisAbaba.Id;
        route.ToCityId = toCity.Id;
        route.DistanceKm = toCity.DistanceFromAddisKm;
        route.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);
        return await MapByIdAsync(route.Id, cancellationToken);
    }

    public async Task<ErrorOr<RouteSeatMapsResponse>> GetSeatMapsByDestinationAsync(
        Guid destinationCityId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var addisAbaba = await GetAddisAbabaAsync(cancellationToken);
        if (addisAbaba is null)
        {
            return DomainErrors.AddisAbabaNotFound;
        }

        if (destinationCityId == addisAbaba.Id)
        {
            return DomainErrors.DestinationCityRequired;
        }

        var route = await db.Routes.AsNoTracking()
            .Include(x => x.FromCity)
            .Include(x => x.ToCity)
            .SingleOrDefaultAsync(
                x => x.IsActive
                     && x.FromCityId == addisAbaba.Id
                     && x.ToCityId == destinationCityId,
                cancellationToken);

        if (route is null)
        {
            return DomainErrors.RouteNotFound;
        }

        return await BuildSeatMapsResponseAsync(route, date, cancellationToken);
    }

    private async Task<ErrorOr<RouteSeatMapsResponse>> BuildSeatMapsResponseAsync(
        Route route,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var tariff = await db.Tariffs.AsNoTracking().SingleOrDefaultAsync(x => x.IsActive, cancellationToken);
        if (tariff is null)
        {
            return DomainErrors.TariffNotFound;
        }

        var (dayStart, dayEnd) = clock.GetUtcRangeForLocalDate(date);
        var ticketPrice = route.DistanceKm * tariff.RatePerKm;

        var schedules = await db.Schedules.AsNoTracking()
            .Include(x => x.Bus)
            .Where(x => x.RouteId == route.Id
                        && x.DepartureAt >= dayStart
                        && x.DepartureAt < dayEnd
                        && (x.Status == ScheduleStatus.Scheduled || x.Status == ScheduleStatus.Boarding))
            .OrderBy(x => x.SequenceNumber)
            .ThenBy(x => x.DepartureAt)
            .ToListAsync(cancellationToken);

        if (schedules.Count == 0)
        {
            return new RouteSeatMapsResponse(
                route.Id,
                route.FromCity.Name,
                route.ToCity.Name,
                route.ToCityId,
                route.DistanceKm,
                date,
                []);
        }

        var scheduleIds = schedules.Select(x => x.Id).ToList();
        var soldSeats = await db.Tickets.AsNoTracking()
            .Where(x => scheduleIds.Contains(x.ScheduleId))
            .Select(x => new { x.ScheduleId, x.SeatNumber })
            .ToListAsync(cancellationToken);

        var soldBySchedule = soldSeats
            .GroupBy(x => x.ScheduleId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.SeatNumber).ToHashSet());

        var seatMaps = schedules.Select(schedule =>
        {
            soldBySchedule.TryGetValue(schedule.Id, out var soldSet);
            soldSet ??= [];

            var summary = SeatMapBuilder.Build(schedule.Bus.SeatCount, soldSet);

            return new RouteScheduleSeatMapResponse(
                schedule.Id,
                schedule.BusId,
                schedule.Bus.PlateNumber,
                schedule.Bus.SideNumber,
                clock.ToLocalDateTime(schedule.DepartureAt),
                schedule.SequenceNumber,
                schedule.Bus.SeatCount,
                summary.SoldSeatCount,
                summary.AvailableSeatCount,
                summary.IsFullySold,
                ticketPrice,
                summary.Seats);
        }).ToList();

        return new RouteSeatMapsResponse(
            route.Id,
            route.FromCity.Name,
            route.ToCity.Name,
            route.ToCityId,
            route.DistanceKm,
            date,
            seatMaps);
    }

    private async Task<City?> GetAddisAbabaAsync(CancellationToken cancellationToken) =>
        await db.Cities.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Name == CityNames.AddisAbaba && x.IsActive, cancellationToken);

    private async Task<ErrorOr<RouteResponse>> MapByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var route = await db.Routes.AsNoTracking()
            .Include(x => x.FromCity)
            .Include(x => x.ToCity)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return route is null ? DomainErrors.RouteNotFound : Map(route);
    }

    private RouteResponse Map(Route route) =>
        new(
            route.Id,
            route.FromCityId,
            route.FromCity.Name,
            route.ToCityId,
            route.ToCity.Name,
            route.DistanceKm,
            route.IsActive,
            clock.ToLocalDateTime(route.CreatedAt));
}
