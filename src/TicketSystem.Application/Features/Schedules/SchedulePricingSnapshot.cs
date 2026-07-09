namespace TicketSystem.Application.Features.Schedules;

using ErrorOr;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Features.Tariffs;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

public static class SchedulePricingSnapshot
{
    public static async Task<ErrorOr<Success>> ApplyFromTariffAsync(
        Schedule schedule,
        Route route,
        IApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        var tariffResult = await TariffResolver.ResolveActiveForRouteAsync(
            db,
            route.Id,
            schedule.BusLevelId,
            schedule.BusTypeId,
            cancellationToken);
        if (tariffResult.IsError)
        {
            return tariffResult.Errors;
        }

        var tariff = tariffResult.Value;
        schedule.TariffId = tariff.Id;
        schedule.ResolvedRatePerKm = tariff.RatePerKm;
        schedule.ResolvedDistanceKm = route.DistanceKm;
        schedule.ResolvedTicketPrice = route.DistanceKm * tariff.RatePerKm;
        schedule.PriceResolutionMode = PriceResolutionMode.Rule;

        return Result.Success;
    }
}
