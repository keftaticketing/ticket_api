namespace TicketSystem.Application.Features.Tariffs;

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Errors;
using TicketSystem.Domain.Entities;

internal static class TariffResolver
{
    public static async Task<ErrorOr<Tariff>> ResolveActiveForBusAsync(
        IApplicationDbContext db,
        Guid busLevelId,
        Guid busTypeId,
        CancellationToken cancellationToken = default)
    {
        var tariff = await db.Tariffs.AsNoTracking()
            .Include(x => x.BusLevel)
            .Include(x => x.BusType)
            .Where(x => x.IsActive && x.BusLevelId == busLevelId && x.BusTypeId == busTypeId)
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        return tariff is null ? DomainErrors.TariffNotFound : tariff;
    }
}
