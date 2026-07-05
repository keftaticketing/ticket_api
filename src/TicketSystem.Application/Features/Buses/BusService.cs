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
    private const string DefaultAssociationCode = "DEFAULT_ASSOC";
    private const string DefaultBusLevelCode = "L1";
    private const string DefaultBusTypeCode = "regular";

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

        var associationResult = await ResolveAssociationAsync(request.AssociationId, cancellationToken);
        if (associationResult.IsError)
        {
            return associationResult.Errors;
        }

        var busLevelResult = await ResolveBusLevelAsync(request.BusLevelId, cancellationToken);
        if (busLevelResult.IsError)
        {
            return busLevelResult.Errors;
        }

        var busTypeResult = await ResolveBusTypeAsync(request.BusTypeId, cancellationToken);
        if (busTypeResult.IsError)
        {
            return busTypeResult.Errors;
        }

        var association = associationResult.Value;
        var busLevel = busLevelResult.Value;
        var busType = busTypeResult.Value;

        var bus = new Bus
        {
            Id = Guid.NewGuid(),
            OwnerName = request.OwnerName.Trim(),
            OwnerPhone = request.OwnerPhone.Trim(),
            DelegatePhone = request.DelegatePhone.Trim(),
            SideNumber = request.SideNumber.Trim(),
            PlateNumber = request.PlateNumber.Trim(),
            SeatCount = request.SeatCount,
            AssociationId = association.Id,
            BusLevelId = busLevel.Id,
            BusTypeId = busType.Id
        };

        db.Buses.Add(bus);
        await db.SaveChangesAsync(cancellationToken);

        bus.Association = association;
        bus.BusLevel = busLevel;
        bus.BusType = busType;
        return Map(bus);
    }

    public async Task<ErrorOr<IReadOnlyList<BusResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var buses = await QueryWithReferences()
            .OrderBy(x => x.PlateNumber)
            .ToListAsync(cancellationToken);

        return buses.Select(Map).ToList();
    }

    public async Task<ErrorOr<BusResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var bus = await QueryWithReferences()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return bus is null ? DomainErrors.BusNotFound : Map(bus);
    }

    public async Task<ErrorOr<BusResponse>> UpdateAsync(Guid id, UpdateBusRequest request, CancellationToken cancellationToken = default)
    {
        if (request.SeatCount < 1)
        {
            return DomainErrors.InvalidSeatCount;
        }

        var bus = await db.Buses
            .Include(x => x.Association)
            .Include(x => x.BusLevel)
            .Include(x => x.BusType)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
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

        if (request.AssociationId is Guid associationId)
        {
            var associationResult = await ResolveAssociationAsync(associationId, cancellationToken);
            if (associationResult.IsError)
            {
                return associationResult.Errors;
            }

            bus.AssociationId = associationResult.Value.Id;
            bus.Association = associationResult.Value;
        }

        if (request.BusLevelId is Guid busLevelId)
        {
            var busLevelResult = await ResolveBusLevelAsync(busLevelId, cancellationToken);
            if (busLevelResult.IsError)
            {
                return busLevelResult.Errors;
            }

            bus.BusLevelId = busLevelResult.Value.Id;
            bus.BusLevel = busLevelResult.Value;
        }

        if (request.BusTypeId is Guid busTypeId)
        {
            var busTypeResult = await ResolveBusTypeAsync(busTypeId, cancellationToken);
            if (busTypeResult.IsError)
            {
                return busTypeResult.Errors;
            }

            bus.BusTypeId = busTypeResult.Value.Id;
            bus.BusType = busTypeResult.Value;
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

    private IQueryable<Bus> QueryWithReferences() =>
        db.Buses.AsNoTracking()
            .Include(x => x.Association)
            .Include(x => x.BusLevel)
            .Include(x => x.BusType);

    private async Task<ErrorOr<Association>> ResolveAssociationAsync(
        Guid? associationId,
        CancellationToken cancellationToken)
    {
        if (associationId is Guid explicitId)
        {
            var association = await db.Associations.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == explicitId && x.IsActive, cancellationToken);
            return association is null ? DomainErrors.AssociationNotFound : association;
        }

        var defaultAssociation = await db.Associations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == DefaultAssociationCode && x.IsActive, cancellationToken);
        return defaultAssociation is null ? DomainErrors.AssociationNotFound : defaultAssociation;
    }

    private async Task<ErrorOr<BusLevel>> ResolveBusLevelAsync(
        Guid? busLevelId,
        CancellationToken cancellationToken)
    {
        if (busLevelId is Guid explicitId)
        {
            var busLevel = await db.BusLevels.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == explicitId && x.IsActive, cancellationToken);
            return busLevel is null ? DomainErrors.BusLevelNotFound : busLevel;
        }

        var defaultBusLevel = await db.BusLevels.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == DefaultBusLevelCode && x.IsActive, cancellationToken);
        return defaultBusLevel is null ? DomainErrors.BusLevelNotFound : defaultBusLevel;
    }

    private async Task<ErrorOr<BusType>> ResolveBusTypeAsync(
        Guid? busTypeId,
        CancellationToken cancellationToken)
    {
        if (busTypeId is Guid explicitId)
        {
            var busType = await db.BusTypes.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == explicitId && x.IsActive, cancellationToken);
            return busType is null ? DomainErrors.BusTypeNotFound : busType;
        }

        var defaultBusType = await db.BusTypes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == DefaultBusTypeCode && x.IsActive, cancellationToken);
        return defaultBusType is null ? DomainErrors.BusTypeNotFound : defaultBusType;
    }

    private BusResponse Map(Bus bus) =>
        new(
            bus.Id,
            bus.OwnerName,
            bus.OwnerPhone,
            bus.DelegatePhone,
            bus.SideNumber,
            bus.PlateNumber,
            bus.SeatCount,
            new BusAssociationResponse(bus.Association.Id, bus.Association.Name, bus.Association.Code),
            new BusLevelReferenceResponse(bus.BusLevel.Id, bus.BusLevel.Code, bus.BusLevel.Name, bus.BusLevel.Rank),
            new BusTypeReferenceResponse(bus.BusType.Id, bus.BusType.Code, bus.BusType.Name),
            bus.IsActive,
            clock.ToLocalDateTime(bus.CreatedAt));
}
