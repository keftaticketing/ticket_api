namespace TicketSystem.Application.Features.Buses;

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Errors;
using TicketSystem.Contracts.Buses;
using TicketSystem.Domain.Entities;

public interface IBusService
{
    Task<ErrorOr<BusResponse>> CreateAsync(CreateBusRequest request, CancellationToken cancellationToken = default);
    Task<ErrorOr<IReadOnlyList<BusResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<BusResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ErrorOr<BusResponse>> UpdateAsync(Guid id, UpdateBusRequest request, CancellationToken cancellationToken = default);
}

public sealed class BusService(IApplicationDbContext db, IBusinessClock clock) : IBusService
{
    public async Task<ErrorOr<BusResponse>> CreateAsync(CreateBusRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SeatCount < 1)
        {
            return DomainErrors.InvalidSeatCount;
        }

        if (await db.Buses.AnyAsync(x => x.PlateNumber == request.PlateNumber, cancellationToken))
        {
            return DomainErrors.DuplicatePlateNumber;
        }

        if (await db.Buses.AnyAsync(x => x.SideNumber == request.SideNumber, cancellationToken))
        {
            return DomainErrors.DuplicateSideNumber;
        }

        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            OwnerName = request.OwnerName.Trim(),
            OwnerPhone = request.OwnerPhone.Trim(),
            DelegatePhone = request.DelegatePhone.Trim(),
            SideNumber = request.SideNumber.Trim(),
            PlateNumber = request.PlateNumber.Trim(),
            SeatCount = request.SeatCount
        };

        db.Buses.Add(bus);
        await db.SaveChangesAsync(cancellationToken);
        return Map(bus);
    }

    public async Task<ErrorOr<IReadOnlyList<BusResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var buses = await db.Buses.AsNoTracking().OrderBy(x => x.PlateNumber).ToListAsync(cancellationToken);
        return buses.Select(Map).ToList();
    }

    public async Task<ErrorOr<BusResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bus = await db.Buses.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return bus is null ? DomainErrors.BusNotFound : Map(bus);
    }

    public async Task<ErrorOr<BusResponse>> UpdateAsync(Guid id, UpdateBusRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SeatCount < 1)
        {
            return DomainErrors.InvalidSeatCount;
        }

        var bus = await db.Buses.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (bus is null)
        {
            return DomainErrors.BusNotFound;
        }

        if (await db.Buses.AnyAsync(x => x.Id != id && x.PlateNumber == request.PlateNumber, cancellationToken))
        {
            return DomainErrors.DuplicatePlateNumber;
        }

        if (await db.Buses.AnyAsync(x => x.Id != id && x.SideNumber == request.SideNumber, cancellationToken))
        {
            return DomainErrors.DuplicateSideNumber;
        }

        bus.OwnerName = request.OwnerName.Trim();
        bus.OwnerPhone = request.OwnerPhone.Trim();
        bus.DelegatePhone = request.DelegatePhone.Trim();
        bus.SideNumber = request.SideNumber.Trim();
        bus.PlateNumber = request.PlateNumber.Trim();
        bus.SeatCount = request.SeatCount;
        bus.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);
        return Map(bus);
    }

    private BusResponse Map(Bus bus) =>
        new(bus.Id, bus.OwnerName, bus.OwnerPhone, bus.DelegatePhone, bus.SideNumber, bus.PlateNumber,
            bus.SeatCount, bus.IsActive, clock.ToLocalDateTime(bus.CreatedAt));
}
