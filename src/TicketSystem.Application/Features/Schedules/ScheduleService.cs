namespace TicketSystem.Application.Features.Schedules;

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Abstractions.Realtime;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Errors;
using TicketSystem.Application.Features.Auth;
using TicketSystem.Contracts.Schedules;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

public interface IScheduleService
{
    Task<ErrorOr<ScheduleResponse>> CreateAsync(CreateScheduleRequest request, CancellationToken cancellationToken = default);
    Task<ErrorOr<IReadOnlyList<ScheduleResponse>>> GetAllAsync(
        Guid? routeId,
        DateOnly? date,
        Guid? scopedFromStationId = null,
        CancellationToken cancellationToken = default);
    Task<ErrorOr<IReadOnlyList<ScheduleResponse>>> GetAvailableAsync(
        Guid routeId,
        DateOnly date,
        Guid? scopedFromStationId = null,
        CancellationToken cancellationToken = default);
    Task<ErrorOr<ScheduleResponse>> UpdateAsync(Guid id, UpdateScheduleRequest request, CancellationToken cancellationToken = default);
    Task<ErrorOr<ScheduleResponse>> SetPriceOverrideAsync(
        Guid id,
        SetSchedulePriceOverrideRequest request,
        Guid adminUserId,
        CancellationToken cancellationToken = default);
    Task<ErrorOr<ScheduleResponse>> ClearPriceOverrideAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ErrorOr<SeatMapResponse>> GetSeatMapAsync(
        Guid scheduleId,
        Guid? scopedFromStationId = null,
        CancellationToken cancellationToken = default);
    Task<ErrorOr<SeatStatusResponse>> GetSeatStatusAsync(
        Guid scheduleId,
        int seatNumber,
        Guid? scopedFromStationId = null,
        CancellationToken cancellationToken = default);
}

public sealed class ScheduleService(
    IApplicationDbContext db,
    IBusinessClock clock,
    ISeatEventPublisher seatEvents) : IScheduleService
{
    public async Task<ErrorOr<ScheduleResponse>> CreateAsync(CreateScheduleRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SequenceNumber < 1)
        {
            return DomainErrors.InvalidSequenceNumber;
        }

        var route = await db.Routes.SingleOrDefaultAsync(x => x.Id == request.RouteId && x.IsActive, cancellationToken);
        if (route is null)
        {
            return DomainErrors.RouteInactive;
        }

        var bus = await db.Buses.SingleOrDefaultAsync(x => x.Id == request.BusId && x.IsActive, cancellationToken);
        if (bus is null)
        {
            return DomainErrors.BusInactive;
        }

        var departureUtc = clock.ToUtcFromLocal(request.DepartureAt);
        var departureDate = clock.ToLocalDate(departureUtc);
        var (dayStart, dayEnd) = clock.GetUtcRangeForLocalDate(departureDate);

        if (await db.Schedules.AnyAsync(
                x => x.BusId == request.BusId
                     && x.DepartureAt >= dayStart
                     && x.DepartureAt < dayEnd
                     && x.Status != ScheduleStatus.Cancelled,
                cancellationToken))
        {
            return DomainErrors.BusAlreadyScheduled;
        }

        if (await db.Schedules.AnyAsync(
                x => x.RouteId == request.RouteId
                     && x.DepartureDate == departureDate
                     && x.AssociationId == bus.AssociationId
                     && x.BusLevelId == bus.BusLevelId
                     && x.BusTypeId == bus.BusTypeId
                     && x.SequenceNumber == request.SequenceNumber
                     && x.Status != ScheduleStatus.Cancelled,
                cancellationToken))
        {
            return DomainErrors.DuplicateSequence;
        }

        var schedule = new Schedule
        {
            Id = Guid.NewGuid(),
            RouteId = route.Id,
            BusId = bus.Id,
            DepartureAt = departureUtc,
            SequenceNumber = request.SequenceNumber
        };
        ScheduleOptionOrdering.ApplyClassificationSnapshot(schedule, bus, departureDate);

        var pricingResult = await SchedulePricingSnapshot.ApplyFromTariffAsync(schedule, route, db, cancellationToken);
        if (pricingResult.IsError)
        {
            return pricingResult.Errors;
        }

        db.Schedules.Add(schedule);
        await db.SaveChangesAsync(cancellationToken);

        return await MapScheduleAsync(schedule.Id, cancellationToken);
    }

    public async Task<ErrorOr<IReadOnlyList<ScheduleResponse>>> GetAllAsync(
        Guid? routeId,
        DateOnly? date,
        Guid? scopedFromStationId = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Schedules.AsNoTracking().Include(x => x.Route).AsQueryable();

        if (routeId.HasValue)
        {
            query = query.Where(x => x.RouteId == routeId.Value);
        }

        if (scopedFromStationId is Guid fromStationId)
        {
            query = query.Where(x => x.Route.FromStationId == fromStationId);
        }

        if (date.HasValue)
        {
            query = query.Where(x => x.DepartureDate == date.Value);
        }

        var ids = await query.OrderForOptionDisplay()
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        return await MapSchedulesAsync(ids, cancellationToken);
    }

    public async Task<ErrorOr<IReadOnlyList<ScheduleResponse>>> GetAvailableAsync(
        Guid routeId,
        DateOnly date,
        Guid? scopedFromStationId = null,
        CancellationToken cancellationToken = default)
    {
        var route = await db.Routes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == routeId && x.IsActive, cancellationToken);
        if (route is null)
        {
            return DomainErrors.RouteInactive;
        }

        var scopeResult = TicketerSellingScope.EnsureRouteOriginMatches(route.FromStationId, scopedFromStationId);
        if (scopeResult.IsError)
        {
            return scopeResult.Errors;
        }

        var ids = await db.Schedules.AsNoTracking()
            .Where(x => x.RouteId == routeId
                        && x.DepartureDate == date
                        && x.Status == ScheduleStatus.Scheduled)
            .OrderForOptionDisplay()
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        return await MapSchedulesAsync(ids, cancellationToken);
    }

    public async Task<ErrorOr<ScheduleResponse>> UpdateAsync(
        Guid id,
        UpdateScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SequenceNumber < 1)
        {
            return DomainErrors.InvalidSequenceNumber;
        }

        if (!Enum.TryParse<ScheduleStatus>(request.Status, true, out var status))
        {
            return DomainErrors.InvalidScheduleStatus;
        }

        var schedule = await db.Schedules.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (schedule is null)
        {
            return DomainErrors.ScheduleNotFound;
        }

        var departureUtc = clock.ToUtcFromLocal(request.DepartureAt);
        var departureDate = clock.ToLocalDate(departureUtc);
        var (dayStart, dayEnd) = clock.GetUtcRangeForLocalDate(departureDate);

        if (status != ScheduleStatus.Cancelled)
        {
            if (await db.Schedules.AnyAsync(
                    x => x.Id != id
                         && x.BusId == schedule.BusId
                         && x.DepartureAt >= dayStart
                         && x.DepartureAt < dayEnd
                         && x.Status != ScheduleStatus.Cancelled,
                    cancellationToken))
            {
                return DomainErrors.BusAlreadyScheduled;
            }

            if (await db.Schedules.AnyAsync(
                    x => x.Id != id
                         && x.RouteId == schedule.RouteId
                         && x.DepartureDate == departureDate
                         && x.AssociationId == schedule.AssociationId
                         && x.BusLevelId == schedule.BusLevelId
                         && x.BusTypeId == schedule.BusTypeId
                         && x.SequenceNumber == request.SequenceNumber
                         && x.Status != ScheduleStatus.Cancelled,
                    cancellationToken))
            {
                return DomainErrors.DuplicateSequence;
            }
        }

        schedule.DepartureAt = departureUtc;
        schedule.DepartureDate = departureDate;
        schedule.SequenceNumber = request.SequenceNumber;
        schedule.Status = status;

        await db.SaveChangesAsync(cancellationToken);

        seatEvents.PublishScheduleUpdated(
            schedule.Id,
            schedule.RouteId,
            clock.ToLocalDate(schedule.DepartureAt),
            status.ToString());

        return await MapScheduleAsync(schedule.Id, cancellationToken);
    }

    public async Task<ErrorOr<ScheduleResponse>> SetPriceOverrideAsync(
        Guid id,
        SetSchedulePriceOverrideRequest request,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return DomainErrors.ManualPriceOverrideRequiresReason;
        }

        if (request.TicketPrice <= 0)
        {
            return DomainErrors.InvalidManualTicketPrice;
        }

        var schedule = await db.Schedules
            .Include(x => x.Route)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (schedule is null)
        {
            return DomainErrors.ScheduleNotFound;
        }

        if (await db.Tickets.AnyAsync(x => x.ScheduleId == id, cancellationToken))
        {
            return DomainErrors.ManualPriceOverrideNotAllowedWhenTicketsSold;
        }

        schedule.ResolvedTicketPrice = request.TicketPrice;
        schedule.ResolvedRatePerKm = schedule.ResolvedDistanceKm == 0
            ? 0
            : decimal.Round(request.TicketPrice / schedule.ResolvedDistanceKm, 2, MidpointRounding.AwayFromZero);
        schedule.PriceResolutionMode = PriceResolutionMode.ManualOverride;
        schedule.ManualPriceOverrideReason = request.Reason.Trim();
        schedule.ManualPriceOverrideByUserId = adminUserId;
        schedule.ManualPriceOverrideAt = clock.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return await MapScheduleAsync(schedule.Id, cancellationToken);
    }

    public async Task<ErrorOr<ScheduleResponse>> ClearPriceOverrideAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var schedule = await db.Schedules
            .Include(x => x.Route)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (schedule is null)
        {
            return DomainErrors.ScheduleNotFound;
        }

        if (schedule.PriceResolutionMode != PriceResolutionMode.ManualOverride)
        {
            return DomainErrors.SchedulePriceOverrideNotApplied;
        }

        if (await db.Tickets.AnyAsync(x => x.ScheduleId == id, cancellationToken))
        {
            return DomainErrors.ManualPriceOverrideNotAllowedWhenTicketsSold;
        }

        var pricingResult = await SchedulePricingSnapshot.ApplyFromTariffAsync(schedule, schedule.Route, db, cancellationToken);
        if (pricingResult.IsError)
        {
            return pricingResult.Errors;
        }

        schedule.ManualPriceOverrideReason = null;
        schedule.ManualPriceOverrideByUserId = null;
        schedule.ManualPriceOverrideAt = null;

        await db.SaveChangesAsync(cancellationToken);
        return await MapScheduleAsync(schedule.Id, cancellationToken);
    }

    public async Task<ErrorOr<SeatMapResponse>> GetSeatMapAsync(
        Guid scheduleId,
        Guid? scopedFromStationId = null,
        CancellationToken cancellationToken = default)
    {
        var schedule = await db.Schedules.AsNoTracking()
            .Include(x => x.Bus)
            .Include(x => x.Route)
            .SingleOrDefaultAsync(x => x.Id == scheduleId, cancellationToken);

        if (schedule is null)
        {
            return DomainErrors.ScheduleNotFound;
        }

        var scopeResult = TicketerSellingScope.EnsureRouteOriginMatches(
            schedule.Route.FromStationId,
            scopedFromStationId);
        if (scopeResult.IsError)
        {
            return scopeResult.Errors;
        }

        if (schedule.Status == ScheduleStatus.Cancelled)
        {
            return DomainErrors.ScheduleCancelled;
        }

        var soldSeats = await db.Tickets.AsNoTracking()
            .Where(x => x.ScheduleId == scheduleId)
            .Select(x => x.SeatNumber)
            .ToListAsync(cancellationToken);

        var summary = SeatMapBuilder.Build(schedule.Bus.SeatCount, soldSeats);

        return new SeatMapResponse(
            scheduleId,
            schedule.Bus.SeatCount,
            summary.SoldSeatCount,
            summary.AvailableSeatCount,
            summary.IsFullySold,
            schedule.ResolvedTicketPrice,
            summary.Seats);
    }

    public async Task<ErrorOr<SeatStatusResponse>> GetSeatStatusAsync(
        Guid scheduleId,
        int seatNumber,
        Guid? scopedFromStationId = null,
        CancellationToken cancellationToken = default)
    {
        var schedule = await db.Schedules.AsNoTracking()
            .Include(x => x.Bus)
            .Include(x => x.Route)
            .SingleOrDefaultAsync(x => x.Id == scheduleId, cancellationToken);

        if (schedule is null)
        {
            return DomainErrors.ScheduleNotFound;
        }

        var scopeResult = TicketerSellingScope.EnsureRouteOriginMatches(
            schedule.Route.FromStationId,
            scopedFromStationId);
        if (scopeResult.IsError)
        {
            return scopeResult.Errors;
        }

        if (schedule.Status == ScheduleStatus.Cancelled)
        {
            return DomainErrors.ScheduleCancelled;
        }

        if (seatNumber < 1 || seatNumber > schedule.Bus.SeatCount)
        {
            return DomainErrors.InvalidSeatNumber(schedule.Bus.SeatCount);
        }

        var soldSeats = await db.Tickets.AsNoTracking()
            .Where(x => x.ScheduleId == scheduleId)
            .Select(x => x.SeatNumber)
            .ToListAsync(cancellationToken);

        return SeatMapBuilder.BuildSeat(seatNumber, soldSeats);
    }

    private async Task<ErrorOr<IReadOnlyList<ScheduleResponse>>> MapSchedulesAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken cancellationToken)
    {
        var results = new List<ScheduleResponse>();
        foreach (var id in ids)
        {
            var mapped = await MapScheduleAsync(id, cancellationToken);
            if (mapped.IsError)
            {
                return mapped.Errors;
            }

            results.Add(mapped.Value);
        }

        return results;
    }

    private async Task<ErrorOr<ScheduleResponse>> MapScheduleAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        var schedule = await db.Schedules.AsNoTracking()
            .Include(x => x.Route).ThenInclude(x => x.FromCity)
            .Include(x => x.Route).ThenInclude(x => x.ToCity)
            .Include(x => x.Bus)
            .SingleOrDefaultAsync(x => x.Id == scheduleId, cancellationToken);

        if (schedule is null)
        {
            return DomainErrors.ScheduleNotFound;
        }

        var soldCount = await db.Tickets.CountAsync(x => x.ScheduleId == scheduleId, cancellationToken);

        return new ScheduleResponse(
            schedule.Id,
            schedule.RouteId,
            schedule.Route.FromCity.Name,
            schedule.Route.ToCity.Name,
            schedule.ResolvedDistanceKm,
            schedule.BusId,
            schedule.Bus.PlateNumber,
            schedule.Bus.SeatCount,
            clock.ToLocalDateTime(schedule.DepartureAt),
            schedule.SequenceNumber,
            schedule.Status.ToString(),
            soldCount,
            schedule.Bus.SeatCount - soldCount,
            schedule.ResolvedRatePerKm,
            schedule.ResolvedTicketPrice,
            schedule.PriceResolutionMode.ToString());
    }
}
