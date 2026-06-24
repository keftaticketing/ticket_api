namespace TicketSystem.Application.Features.Schedules;

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Abstractions.Realtime;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Errors;
using TicketSystem.Contracts.Schedules;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

public interface IScheduleService
{
    Task<ErrorOr<ScheduleResponse>> CreateAsync(CreateScheduleRequest request, CancellationToken cancellationToken = default);
    Task<ErrorOr<IReadOnlyList<ScheduleResponse>>> GetAllAsync(Guid? routeId, DateOnly? date, CancellationToken cancellationToken = default);
    Task<ErrorOr<IReadOnlyList<ScheduleResponse>>> GetAvailableAsync(Guid routeId, DateOnly date, CancellationToken cancellationToken = default);
    Task<ErrorOr<ScheduleResponse>> UpdateAsync(Guid id, UpdateScheduleRequest request, CancellationToken cancellationToken = default);
    Task<ErrorOr<SeatMapResponse>> GetSeatMapAsync(Guid scheduleId, CancellationToken cancellationToken = default);
    Task<ErrorOr<SeatStatusResponse>> GetSeatStatusAsync(Guid scheduleId, int seatNumber, CancellationToken cancellationToken = default);
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
        var (dayStart, dayEnd) = clock.GetUtcRangeForLocalDate(clock.ToLocalDate(departureUtc));

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
                     && x.DepartureAt >= dayStart
                     && x.DepartureAt < dayEnd
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

        db.Schedules.Add(schedule);
        await db.SaveChangesAsync(cancellationToken);

        return await MapScheduleAsync(schedule.Id, cancellationToken);
    }

    public async Task<ErrorOr<IReadOnlyList<ScheduleResponse>>> GetAllAsync(
        Guid? routeId,
        DateOnly? date,
        CancellationToken cancellationToken = default)
    {
        var query = db.Schedules.AsNoTracking().AsQueryable();

        if (routeId.HasValue)
        {
            query = query.Where(x => x.RouteId == routeId.Value);
        }

        if (date.HasValue)
        {
            var (dayStart, dayEnd) = clock.GetUtcRangeForLocalDate(date.Value);
            query = query.Where(x => x.DepartureAt >= dayStart && x.DepartureAt < dayEnd);
        }

        var ids = await query.OrderBy(x => x.DepartureAt).ThenBy(x => x.SequenceNumber)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        return await MapSchedulesAsync(ids, cancellationToken);
    }

    public async Task<ErrorOr<IReadOnlyList<ScheduleResponse>>> GetAvailableAsync(
        Guid routeId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var (dayStart, dayEnd) = clock.GetUtcRangeForLocalDate(date);

        var ids = await db.Schedules.AsNoTracking()
            .Where(x => x.RouteId == routeId
                        && x.DepartureAt >= dayStart
                        && x.DepartureAt < dayEnd
                        && x.Status == ScheduleStatus.Scheduled)
            .OrderBy(x => x.SequenceNumber)
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
        var (dayStart, dayEnd) = clock.GetUtcRangeForLocalDate(clock.ToLocalDate(departureUtc));

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
                         && x.DepartureAt >= dayStart
                         && x.DepartureAt < dayEnd
                         && x.SequenceNumber == request.SequenceNumber
                         && x.Status != ScheduleStatus.Cancelled,
                    cancellationToken))
            {
                return DomainErrors.DuplicateSequence;
            }
        }

        schedule.DepartureAt = departureUtc;
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

    public async Task<ErrorOr<SeatMapResponse>> GetSeatMapAsync(Guid scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await db.Schedules.AsNoTracking()
            .Include(x => x.Bus)
            .Include(x => x.Route)
            .SingleOrDefaultAsync(x => x.Id == scheduleId, cancellationToken);

        if (schedule is null)
        {
            return DomainErrors.ScheduleNotFound;
        }

        if (schedule.Status == ScheduleStatus.Cancelled)
        {
            return DomainErrors.ScheduleCancelled;
        }

        var tariff = await db.Tariffs.AsNoTracking().SingleOrDefaultAsync(x => x.IsActive, cancellationToken);
        if (tariff is null)
        {
            return DomainErrors.TariffNotFound;
        }

        var soldSeats = await db.Tickets.AsNoTracking()
            .Where(x => x.ScheduleId == scheduleId)
            .Select(x => x.SeatNumber)
            .ToListAsync(cancellationToken);

        var summary = SeatMapBuilder.Build(schedule.Bus.SeatCount, soldSeats);
        var ticketPrice = schedule.Route.DistanceKm * tariff.RatePerKm;

        return new SeatMapResponse(
            scheduleId,
            schedule.Bus.SeatCount,
            summary.SoldSeatCount,
            summary.AvailableSeatCount,
            summary.IsFullySold,
            ticketPrice,
            summary.Seats);
    }

    public async Task<ErrorOr<SeatStatusResponse>> GetSeatStatusAsync(
        Guid scheduleId,
        int seatNumber,
        CancellationToken cancellationToken = default)
    {
        var schedule = await db.Schedules.AsNoTracking()
            .Include(x => x.Bus)
            .SingleOrDefaultAsync(x => x.Id == scheduleId, cancellationToken);

        if (schedule is null)
        {
            return DomainErrors.ScheduleNotFound;
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

        var tariff = await db.Tariffs.AsNoTracking().SingleOrDefaultAsync(x => x.IsActive, cancellationToken);
        if (tariff is null)
        {
            return DomainErrors.TariffNotFound;
        }

        var soldCount = await db.Tickets.CountAsync(x => x.ScheduleId == scheduleId, cancellationToken);
        var ticketPrice = schedule.Route.DistanceKm * tariff.RatePerKm;

        return new ScheduleResponse(
            schedule.Id,
            schedule.RouteId,
            schedule.Route.FromCity.Name,
            schedule.Route.ToCity.Name,
            schedule.Route.DistanceKm,
            schedule.BusId,
            schedule.Bus.PlateNumber,
            schedule.Bus.SeatCount,
            clock.ToLocalDateTime(schedule.DepartureAt),
            schedule.SequenceNumber,
            schedule.Status.ToString(),
            soldCount,
            schedule.Bus.SeatCount - soldCount,
            tariff.RatePerKm,
            ticketPrice);
    }
}
