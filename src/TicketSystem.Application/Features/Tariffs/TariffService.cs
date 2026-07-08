namespace TicketSystem.Application.Features.Tariffs;

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Errors;
using TicketSystem.Contracts.Tariffs;
using TicketSystem.Domain.Entities;

public interface ITariffService
{
    Task<ErrorOr<IReadOnlyList<TariffResponse>>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<TariffResponse>> SetActiveAsync(SetTariffRequest request, CancellationToken cancellationToken = default);
    Task<ErrorOr<IReadOnlyList<TariffResponse>>> GetHistoryAsync(CancellationToken cancellationToken = default);
}

public sealed class TariffService(IApplicationDbContext db, IBusinessClock clock) : ITariffService
{
    public async Task<ErrorOr<IReadOnlyList<TariffResponse>>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var tariffs = await QueryTariffs()
            .Where(x => x.IsActive)
            .OrderBy(x => x.RouteId.HasValue ? 1 : 0)
            .ThenBy(x => x.BusLevel.Rank)
            .ThenBy(x => x.BusType.Name)
            .ThenByDescending(x => x.EffectiveFrom)
            .ToListAsync(cancellationToken);

        return tariffs.Count == 0
            ? DomainErrors.TariffNotFound
            : tariffs.Select(Map).ToList();
    }

    public async Task<ErrorOr<TariffResponse>> SetActiveAsync(SetTariffRequest request, CancellationToken cancellationToken = default)
    {
        if (request.RatePerKm <= 0)
        {
            return DomainErrors.InvalidRatePerKm;
        }

        var busLevel = await db.BusLevels
            .SingleOrDefaultAsync(x => x.Id == request.BusLevelId && x.IsActive, cancellationToken);
        if (busLevel is null)
        {
            return DomainErrors.BusLevelNotFound;
        }

        var busType = await db.BusTypes
            .SingleOrDefaultAsync(x => x.Id == request.BusTypeId && x.IsActive, cancellationToken);
        if (busType is null)
        {
            return DomainErrors.BusTypeNotFound;
        }

        Route? route = null;
        if (request.RouteId is Guid routeId)
        {
            route = await db.Routes.AsNoTracking()
                .Include(x => x.FromCity)
                .Include(x => x.ToCity)
                .SingleOrDefaultAsync(x => x.Id == routeId && x.IsActive, cancellationToken);
            if (route is null)
            {
                return DomainErrors.RouteNotFound;
            }
        }

        var now = clock.UtcNow;
        var current = await db.Tariffs.SingleOrDefaultAsync(
            x => x.IsActive
                 && x.BusLevelId == request.BusLevelId
                 && x.BusTypeId == request.BusTypeId
                 && x.RouteId == request.RouteId,
            cancellationToken);
        if (current is not null)
        {
            current.IsActive = false;
            current.EffectiveTo = now;
        }

        var tariff = new Tariff
        {
            Id = Guid.NewGuid(),
            RouteId = request.RouteId,
            BusLevelId = busLevel.Id,
            BusTypeId = busType.Id,
            RatePerKm = request.RatePerKm,
            Currency = "ETB",
            IsActive = true,
            EffectiveFrom = now,
            BusLevel = busLevel,
            BusType = busType
        };

        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync(cancellationToken);

        if (route is not null)
        {
            tariff.Route = route;
        }

        return Map(tariff);
    }

    public async Task<ErrorOr<IReadOnlyList<TariffResponse>>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        var tariffs = await QueryTariffs()
            .OrderByDescending(x => x.EffectiveFrom)
            .ToListAsync(cancellationToken);
        return tariffs.Select(Map).ToList();
    }

    private IQueryable<Tariff> QueryTariffs() =>
        db.Tariffs.AsNoTracking()
            .Include(x => x.BusLevel)
            .Include(x => x.BusType)
            .Include(x => x.Route).ThenInclude(x => x!.FromCity)
            .Include(x => x.Route).ThenInclude(x => x!.ToCity);

    private TariffResponse Map(Tariff tariff) =>
        new(
            tariff.Id,
            tariff.RouteId,
            tariff.Route is null
                ? null
                : new TariffRouteResponse(
                    tariff.Route.Id,
                    tariff.Route.FromCity.Name,
                    tariff.Route.ToCity.Name),
            new TariffBusLevelResponse(
                tariff.BusLevel.Id,
                tariff.BusLevel.Code,
                tariff.BusLevel.Name,
                tariff.BusLevel.Rank),
            new TariffBusTypeResponse(
                tariff.BusType.Id,
                tariff.BusType.Code,
                tariff.BusType.Name),
            tariff.RatePerKm,
            tariff.Currency,
            tariff.IsActive,
            clock.ToLocalDateTime(tariff.EffectiveFrom),
            tariff.EffectiveTo is null ? null : clock.ToLocalDateTime(tariff.EffectiveTo.Value));
}
