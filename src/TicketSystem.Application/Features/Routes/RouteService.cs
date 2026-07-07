namespace TicketSystem.Application.Features.Routes;

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Features.Auth;
using TicketSystem.Application.Features.Schedules;
using TicketSystem.Application.Features.Tariffs;
using TicketSystem.Application.Errors;
using TicketSystem.Contracts.Routes;
using TicketSystem.Contracts.Schedules;
using TicketSystem.Domain.Common;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

public interface IRouteService
{
    Task<ErrorOr<RouteResponse>> CreateAsync(CreateRouteRequest request, CancellationToken cancellationToken = default);
    Task<ErrorOr<IReadOnlyList<RouteResponse>>> GetAllAsync(
        Guid? toCityId,
        Guid? fromStationId,
        CancellationToken cancellationToken = default);
    Task<ErrorOr<RouteResponse>> GetByIdAsync(
        Guid id,
        Guid? scopedFromStationId = null,
        CancellationToken cancellationToken = default);
    Task<ErrorOr<RouteResponse>> UpdateAsync(Guid id, UpdateRouteRequest request, CancellationToken cancellationToken = default);
    Task<ErrorOr<RouteSeatMapsResponse>> GetSeatMapsByDestinationAsync(
        Guid destinationCityId,
        DateOnly date,
        Guid? fromStationId,
        CancellationToken cancellationToken = default);
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

        var fromStationResult = await ResolveStationForCityAsync(addisAbaba.Id, null, cancellationToken);
        if (fromStationResult.IsError)
        {
            return fromStationResult.Errors;
        }

        var toStationResult = await ResolveStationForCityAsync(toCity.Id, request.ToStationId, cancellationToken);
        if (toStationResult.IsError)
        {
            return toStationResult.Errors;
        }

        var fromStation = fromStationResult.Value;
        var toStation = toStationResult.Value;

        if (await db.Routes.AnyAsync(
                x => x.FromStationId == fromStation.Id && x.ToStationId == toStation.Id,
                cancellationToken))
        {
            return DomainErrors.DuplicateRoute;
        }

        var route = new Route
        {
            Id = Guid.NewGuid(),
            FromCityId = addisAbaba.Id,
            FromStationId = fromStation.Id,
            ToCityId = toCity.Id,
            ToStationId = toStation.Id,
            DistanceKm = toCity.DistanceFromAddisKm
        };

        db.Routes.Add(route);
        await db.SaveChangesAsync(cancellationToken);
        return await MapByIdAsync(route.Id, cancellationToken);
    }

    public async Task<ErrorOr<IReadOnlyList<RouteResponse>>> GetAllAsync(
        Guid? toCityId,
        Guid? fromStationId,
        CancellationToken cancellationToken = default)
    {
        var addisAbaba = await GetAddisAbabaAsync(cancellationToken);
        if (addisAbaba is null)
        {
            return DomainErrors.AddisAbabaNotFound;
        }

        var query = db.Routes.AsNoTracking()
            .Include(x => x.FromCity)
            .Include(x => x.FromStation)
            .ThenInclude(x => x.City)
            .Include(x => x.ToCity)
            .Include(x => x.ToStation)
            .ThenInclude(x => x.City)
            .Where(x => x.IsActive && x.FromCityId == addisAbaba.Id);

        if (toCityId.HasValue)
        {
            query = query.Where(x => x.ToCityId == toCityId.Value);
        }

        if (fromStationId.HasValue)
        {
            query = query.Where(x => x.FromStationId == fromStationId.Value);
        }

        var routes = await query
            .OrderBy(x => x.ToCity.DistanceFromAddisKm)
            .ThenBy(x => x.ToCity.Name)
            .ThenBy(x => x.FromStation.Name)
            .ToListAsync(cancellationToken);

        return routes.Select(Map).ToList();
    }

    public async Task<ErrorOr<RouteResponse>> GetByIdAsync(
        Guid id,
        Guid? scopedFromStationId = null,
        CancellationToken cancellationToken = default)
    {
        var route = await db.Routes.AsNoTracking()
            .Include(x => x.FromCity)
            .Include(x => x.FromStation)
            .ThenInclude(x => x.City)
            .Include(x => x.ToCity)
            .Include(x => x.ToStation)
            .ThenInclude(x => x.City)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsActive, cancellationToken);
        if (route is null)
        {
            return DomainErrors.RouteNotFound;
        }

        var scopeResult = TicketerSellingScope.EnsureRouteOriginMatches(route.FromStationId, scopedFromStationId);
        if (scopeResult.IsError)
        {
            return scopeResult.Errors;
        }

        return Map(route);
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

        var fromStationResult = await ResolveStationForCityAsync(addisAbaba.Id, null, cancellationToken);
        if (fromStationResult.IsError)
        {
            return fromStationResult.Errors;
        }

        var toStationResult = await ResolveStationForCityAsync(toCity.Id, request.ToStationId, cancellationToken);
        if (toStationResult.IsError)
        {
            return toStationResult.Errors;
        }

        var fromStation = fromStationResult.Value;
        var toStation = toStationResult.Value;

        if (await db.Routes.AnyAsync(
                x => x.Id != id
                     && x.FromStationId == fromStation.Id
                     && x.ToStationId == toStation.Id,
                cancellationToken))
        {
            return DomainErrors.DuplicateRoute;
        }

        route.FromCityId = addisAbaba.Id;
        route.FromStationId = fromStation.Id;
        route.ToCityId = toCity.Id;
        route.ToStationId = toStation.Id;
        route.DistanceKm = toCity.DistanceFromAddisKm;
        route.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);
        return await MapByIdAsync(route.Id, cancellationToken);
    }

    public async Task<ErrorOr<RouteSeatMapsResponse>> GetSeatMapsByDestinationAsync(
        Guid destinationCityId,
        DateOnly date,
        Guid? fromStationId,
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

        var routeQuery = db.Routes.AsNoTracking()
            .Include(x => x.FromCity)
            .Include(x => x.FromStation)
            .ThenInclude(x => x.City)
            .Include(x => x.ToCity)
            .Include(x => x.ToStation)
            .ThenInclude(x => x.City)
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
        var schedules = await db.Schedules.AsNoTracking()
            .Include(x => x.Bus)
            .Include(x => x.BusLevel)
            .Include(x => x.BusType)
            .Where(x => x.RouteId == route.Id
                        && x.DepartureDate == date
                        && (x.Status == ScheduleStatus.Scheduled || x.Status == ScheduleStatus.Boarding))
            .OrderForOptionDisplay()
            .ToListAsync(cancellationToken);

        if (schedules.Count == 0)
        {
            return new RouteSeatMapsResponse(
                route.Id,
                route.FromCity.Name,
                route.ToCity.Name,
                MapStation(route.FromStation),
                MapStation(route.ToStation),
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

        var seatMaps = new List<RouteScheduleSeatMapResponse>();
        foreach (var schedule in schedules)
        {
            soldBySchedule.TryGetValue(schedule.Id, out var soldSet);
            soldSet ??= [];

            var summary = SeatMapBuilder.Build(schedule.Bus.SeatCount, soldSet);
            var tariffResult = await TariffResolver.ResolveActiveForBusAsync(
                db,
                schedule.Bus.BusLevelId,
                schedule.Bus.BusTypeId,
                cancellationToken);
            if (tariffResult.IsError)
            {
                return tariffResult.Errors;
            }
            var ticketPrice = route.DistanceKm * tariffResult.Value.RatePerKm;

            seatMaps.Add(new RouteScheduleSeatMapResponse(
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
                summary.Seats));
        }

        return new RouteSeatMapsResponse(
            route.Id,
            route.FromCity.Name,
            route.ToCity.Name,
            MapStation(route.FromStation),
            MapStation(route.ToStation),
            route.ToCityId,
            route.DistanceKm,
            date,
            seatMaps);
    }

    private async Task<ErrorOr<Station>> ResolveStationForCityAsync(
        Guid cityId,
        Guid? stationId,
        CancellationToken cancellationToken)
    {
        if (stationId is Guid explicitStationId)
        {
            var explicitStation = await db.Stations.AsNoTracking()
                .Include(x => x.City)
                .SingleOrDefaultAsync(x => x.Id == explicitStationId && x.IsActive, cancellationToken);
            if (explicitStation is null)
            {
                return DomainErrors.StationNotFound;
            }

            if (explicitStation.CityId != cityId)
            {
                return DomainErrors.StationCityMismatch;
            }

            return explicitStation;
        }

        var defaultStation = await db.Stations
            .Include(x => x.City)
            .SingleOrDefaultAsync(
                x => x.CityId == cityId && x.IsImplicitDefault && x.IsActive,
                cancellationToken);
        if (defaultStation is not null)
        {
            return defaultStation;
        }

        var city = await db.Cities.SingleOrDefaultAsync(x => x.Id == cityId && x.IsActive, cancellationToken);
        if (city is null)
        {
            return DomainErrors.CityInactive;
        }

        defaultStation = CreateImplicitDefaultStation(city);
        db.Stations.Add(defaultStation);
        await db.SaveChangesAsync(cancellationToken);

        return defaultStation;
    }

    private static Station CreateImplicitDefaultStation(City city) =>
        new()
        {
            Id = Guid.NewGuid(),
            CityId = city.Id,
            City = city,
            Name = "Meneharia",
            NameAm = "መነሓሪያ",
            Code = BuildDefaultStationCode(city.Name),
            IsImplicitDefault = true
        };

    private static string BuildDefaultStationCode(string cityName)
    {
        Span<char> buffer = stackalloc char[cityName.Length];
        var len = 0;
        foreach (var ch in cityName.ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                buffer[len++] = ch;
                continue;
            }

            if (len > 0 && buffer[len - 1] != '_')
            {
                buffer[len++] = '_';
            }
        }

        var normalized = new string(buffer[..len]).TrimEnd('_');
        return $"{normalized}_MAIN";
    }

    private async Task<City?> GetAddisAbabaAsync(CancellationToken cancellationToken) =>
        await db.Cities.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Name == CityNames.AddisAbaba && x.IsActive, cancellationToken);

    private async Task<ErrorOr<RouteResponse>> MapByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var route = await db.Routes.AsNoTracking()
            .Include(x => x.FromCity)
            .Include(x => x.FromStation)
            .ThenInclude(x => x.City)
            .Include(x => x.ToCity)
            .Include(x => x.ToStation)
            .ThenInclude(x => x.City)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return route is null ? DomainErrors.RouteNotFound : Map(route);
    }

    private RouteResponse Map(Route route) =>
        new(
            route.Id,
            route.FromCityId,
            route.FromCity.Name,
            MapStation(route.FromStation),
            route.ToCityId,
            route.ToCity.Name,
            MapStation(route.ToStation),
            route.DistanceKm,
            route.IsActive,
            clock.ToLocalDateTime(route.CreatedAt));

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
