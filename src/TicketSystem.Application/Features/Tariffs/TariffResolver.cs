namespace TicketSystem.Application.Features.Tariffs;

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Errors;
using TicketSystem.Domain.Entities;

internal static class TariffResolver
{
    public static Task<ErrorOr<Tariff>> ResolveActiveForBusAsync(
        IApplicationDbContext db,
        Guid busLevelId,
        Guid busTypeId,
        CancellationToken cancellationToken = default) =>
        ResolveActiveForRouteAsync(db, routeId: null, busLevelId, busTypeId, cancellationToken);

    public static async Task<ErrorOr<Tariff>> ResolveActiveForRouteAsync(
        IApplicationDbContext db,
        Guid? routeId,
        Guid busLevelId,
        Guid busTypeId,
        CancellationToken cancellationToken = default)
    {
        if (routeId.HasValue)
        {
            var resolvedRouteId = routeId.Value;
            var routeOverride = await db.Tariffs.AsNoTracking()
                .Include(x => x.BusLevel)
                .Include(x => x.BusType)
                .Where(x => x.IsActive
                            && x.RouteId == resolvedRouteId
                            && x.BusLevelId == busLevelId
                            && x.BusTypeId == busTypeId)
                .OrderByDescending(x => x.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);

            if (routeOverride is not null)
            {
                return routeOverride;
            }
        }

        var defaultRule = await db.Tariffs.AsNoTracking()
            .Include(x => x.BusLevel)
            .Include(x => x.BusType)
            .Where(x => x.IsActive
                        && x.RouteId == null
                        && x.BusLevelId == busLevelId
                        && x.BusTypeId == busTypeId)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        return defaultRule is null ? DomainErrors.TariffNotFound : defaultRule;
    }
}
