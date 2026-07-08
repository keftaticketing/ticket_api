namespace TicketSystem.Application.Features.Tickets;

using System.Data;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Authentication;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Abstractions.Realtime;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Errors;
using TicketSystem.Application.Features.SalesParties;
using TicketSystem.Application.Features.Schedules;
using TicketSystem.Contracts.SalesParties;
using TicketSystem.Contracts.Tickets;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

public interface ITicketService
{
    Task<ErrorOr<SellCashTicketResponse>> SellCashAsync(SellCashTicketRequest request, Guid ticketerId, CancellationToken cancellationToken = default);
    Task<ErrorOr<TicketResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ErrorOr<IReadOnlyList<TicketResponse>>> SearchAsync(Guid? scheduleId, string? passengerPhone, DateOnly? date, CancellationToken cancellationToken = default);
}

public sealed class TicketService(
    IApplicationDbContext db,
    IIdentityUserService identityUserService,
    IIdentityAccountService accountService,
    IBusinessClock clock,
    ITicketSaleDistributionWriter distributionWriter,
    ISeatEventPublisher seatEvents) : ITicketService
{
    public async Task<ErrorOr<SellCashTicketResponse>> SellCashAsync(
        SellCashTicketRequest request,
        Guid ticketerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PassengerName))
        {
            return DomainErrors.PassengerNameRequired;
        }

        if (string.IsNullOrWhiteSpace(request.PassengerPhone))
        {
            return DomainErrors.PassengerPhoneRequired;
        }

        if (!await identityUserService.IsActiveTicketerAsync(ticketerId, cancellationToken))
        {
            return DomainErrors.TicketerRequired;
        }

        var sellingStationResult = await accountService.ResolveSellingStationIdAsync(ticketerId, cancellationToken);
        if (sellingStationResult.IsError)
        {
            return sellingStationResult.Errors;
        }

        var ticketerName = await identityUserService.GetFullNameAsync(ticketerId, cancellationToken)
            ?? "Unknown";

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var schedule = await db.Schedules
            .Include(x => x.Route)
            .Include(x => x.Bus)
            .SingleOrDefaultAsync(x => x.Id == request.ScheduleId, cancellationToken);

        if (schedule is null)
        {
            return DomainErrors.ScheduleNotFound;
        }

        if (schedule.Route.FromStationId != sellingStationResult.Value)
        {
            return DomainErrors.ScheduleOriginStationMismatch;
        }

        if (schedule.Status != ScheduleStatus.Scheduled && schedule.Status != ScheduleStatus.Boarding)
        {
            return DomainErrors.InvalidScheduleForSale;
        }

        if (request.SeatNumber < 1 || request.SeatNumber > schedule.Bus.SeatCount)
        {
            return DomainErrors.InvalidSeatNumber(schedule.Bus.SeatCount);
        }

        if (await db.Tickets.AnyAsync(
                x => x.ScheduleId == request.ScheduleId && x.SeatNumber == request.SeatNumber,
                cancellationToken))
        {
            return DomainErrors.SeatAlreadySold;
        }

        var price = schedule.ResolvedTicketPrice;

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            SeatNumber = request.SeatNumber,
            PassengerName = request.PassengerName.Trim(),
            PassengerPhone = request.PassengerPhone.Trim(),
            NationalId = string.IsNullOrWhiteSpace(request.NationalId) ? null : request.NationalId.Trim(),
            Price = price,
            DistanceKm = schedule.ResolvedDistanceKm,
            RatePerKm = schedule.ResolvedRatePerKm,
            PaymentMethod = PaymentMethod.Cash,
            SoldByUserId = ticketerId,
            SoldByUserName = ticketerName,
            SoldAt = clock.UtcNow
        };

        db.Tickets.Add(ticket);

        TicketCashBreakdownResponse? cashBreakdown = null;

        try
        {
            await distributionWriter.ApplyAsync(ticket, price, cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("deductions exceed"))
        {
            return DomainErrors.BusOwnerDeductionExceedsFare;
        }
        catch (InvalidOperationException)
        {
            return DomainErrors.SalesPartyConfigurationMissing;
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return DomainErrors.SeatAlreadySold;
        }

        var breakdownResult = await GetCashBreakdownAsync(ticket.Id, cancellationToken);
        if (!breakdownResult.IsError)
        {
            cashBreakdown = breakdownResult.Value;
        }

        var mapped = await MapTicketAsync(ticket.Id, cancellationToken);
        if (mapped.IsError)
        {
            return mapped.Errors;
        }

        var soldSeats = await db.Tickets.AsNoTracking()
            .Where(x => x.ScheduleId == schedule.Id)
            .Select(x => x.SeatNumber)
            .ToListAsync(cancellationToken);

        var summary = SeatMapBuilder.Build(schedule.Bus.SeatCount, soldSeats);

        seatEvents.PublishSeatSold(
            schedule.Id,
            schedule.RouteId,
            clock.ToLocalDate(schedule.DepartureAt),
            request.SeatNumber,
            ticket.Id,
            summary.SoldSeatCount,
            summary.AvailableSeatCount,
            summary.IsFullySold);

        return new SellCashTicketResponse(
            mapped.Value,
            summary.SoldSeatCount,
            summary.AvailableSeatCount,
            summary.IsFullySold,
            cashBreakdown);
    }

    private async Task<ErrorOr<TicketCashBreakdownResponse>> GetCashBreakdownAsync(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == ticketId, cancellationToken);
        if (ticket is null)
        {
            return DomainErrors.TicketNotFound;
        }

        var distributions = await db.TicketSaleDistributions.AsNoTracking()
            .Where(x => x.TicketId == ticketId)
            .OrderBy(x => x.PartyCode)
            .ToListAsync(cancellationToken);

        if (distributions.Count == 0)
        {
            return new TicketCashBreakdownResponse(ticket.Price, 0, ticket.Price, []);
        }

        var salesFeeTotal = distributions
            .Where(x => x.Source == SalesPartySource.SalesFee)
            .Sum(x => x.AmountEtb);

        return new TicketCashBreakdownResponse(
            ticket.Price,
            salesFeeTotal,
            ticket.Price,
            distributions.Select(x => new TicketSaleDistributionResponse(
                x.Id,
                x.TicketId,
                x.PartyCode,
                x.PartyName,
                x.Source.ToString(),
                x.AllocationType.ToString(),
                x.AmountEtb,
                clock.ToLocalDateTime(x.CreatedAt))).ToList());
    }

    public async Task<ErrorOr<TicketResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await db.Tickets.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken))
        {
            return DomainErrors.TicketNotFound;
        }

        return await MapTicketAsync(id, cancellationToken);
    }

    public async Task<ErrorOr<IReadOnlyList<TicketResponse>>> SearchAsync(
        Guid? scheduleId,
        string? passengerPhone,
        DateOnly? date,
        CancellationToken cancellationToken = default)
    {
        var query = db.Tickets.AsNoTracking().AsQueryable();

        if (scheduleId.HasValue)
        {
            query = query.Where(x => x.ScheduleId == scheduleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(passengerPhone))
        {
            query = query.Where(x => x.PassengerPhone == passengerPhone.Trim());
        }

        if (date.HasValue)
        {
            var (dayStart, dayEnd) = clock.GetUtcRangeForLocalDate(date.Value);
            query = query.Where(x => x.Schedule.DepartureAt >= dayStart && x.Schedule.DepartureAt < dayEnd);
        }

        var ids = await query.OrderByDescending(x => x.SoldAt).Select(x => x.Id).ToListAsync(cancellationToken);
        var results = new List<TicketResponse>();

        foreach (var id in ids)
        {
            var mapped = await MapTicketAsync(id, cancellationToken);
            if (mapped.IsError)
            {
                return mapped.Errors;
            }

            results.Add(mapped.Value);
        }

        return results;
    }

    private async Task<ErrorOr<TicketResponse>> MapTicketAsync(Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await db.Tickets.AsNoTracking()
            .Include(x => x.Schedule).ThenInclude(x => x.Route).ThenInclude(x => x.FromCity)
            .Include(x => x.Schedule).ThenInclude(x => x.Route).ThenInclude(x => x.ToCity)
            .Include(x => x.Schedule).ThenInclude(x => x.Bus)
            .SingleOrDefaultAsync(x => x.Id == ticketId, cancellationToken);

        if (ticket is null)
        {
            return DomainErrors.TicketNotFound;
        }

        return new TicketResponse(
            ticket.Id,
            ticket.ScheduleId,
            ticket.Schedule.Route.FromCity.Name,
            ticket.Schedule.Route.ToCity.Name,
            clock.ToLocalDateTime(ticket.Schedule.DepartureAt),
            ticket.Schedule.SequenceNumber,
            ticket.Schedule.Bus.PlateNumber,
            ticket.Schedule.Bus.SideNumber,
            ticket.SeatNumber,
            ticket.PassengerName,
            ticket.PassengerPhone,
            ticket.NationalId,
            ticket.Price,
            ticket.DistanceKm,
            ticket.RatePerKm,
            ticket.PaymentMethod.ToString(),
            ticket.SoldByUserName,
            clock.ToLocalDateTime(ticket.SoldAt));
    }
}
