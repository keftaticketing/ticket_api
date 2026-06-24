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
    Task<ErrorOr<TariffResponse>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<TariffResponse>> SetActiveAsync(SetTariffRequest request, CancellationToken cancellationToken = default);
    Task<ErrorOr<IReadOnlyList<TariffResponse>>> GetHistoryAsync(CancellationToken cancellationToken = default);
}

public sealed class TariffService(IApplicationDbContext db, IBusinessClock clock) : ITariffService
{
    public async Task<ErrorOr<TariffResponse>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var tariff = await db.Tariffs.AsNoTracking().SingleOrDefaultAsync(x => x.IsActive, cancellationToken);
        return tariff is null ? DomainErrors.TariffNotFound : Map(tariff);
    }

    public async Task<ErrorOr<TariffResponse>> SetActiveAsync(SetTariffRequest request, CancellationToken cancellationToken = default)
    {
        if (request.RatePerKm <= 0)
        {
            return DomainErrors.InvalidRatePerKm;
        }

        var now = clock.UtcNow;
        var current = await db.Tariffs.SingleOrDefaultAsync(x => x.IsActive, cancellationToken);
        if (current is not null)
        {
            current.IsActive = false;
            current.EffectiveTo = now;
        }

        var tariff = new Tariff
        {
            Id = Guid.NewGuid(),
            RatePerKm = request.RatePerKm,
            Currency = "ETB",
            IsActive = true,
            EffectiveFrom = now
        };

        db.Tariffs.Add(tariff);
        await db.SaveChangesAsync(cancellationToken);
        return Map(tariff);
    }

    public async Task<ErrorOr<IReadOnlyList<TariffResponse>>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        var tariffs = await db.Tariffs.AsNoTracking()
            .OrderByDescending(x => x.EffectiveFrom)
            .ToListAsync(cancellationToken);
        return tariffs.Select(x => Map(x)).ToList();
    }

    private TariffResponse Map(Tariff tariff) =>
        new(
            tariff.Id,
            tariff.RatePerKm,
            tariff.Currency,
            tariff.IsActive,
            clock.ToLocalDateTime(tariff.EffectiveFrom),
            tariff.EffectiveTo is null ? null : clock.ToLocalDateTime(tariff.EffectiveTo.Value));
}
