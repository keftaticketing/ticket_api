namespace TicketSystem.Application.Features.SalesParties;

using ErrorOr;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Persistence;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Errors;
using TicketSystem.Contracts.SalesParties;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

public interface ISalesPartyService
{
    Task<ErrorOr<SalesPartyResponse>> CreateAsync(CreateSalesPartyRequest request, CancellationToken cancellationToken = default);
    Task<ErrorOr<IReadOnlyList<SalesPartyResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<SalesPartyResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ErrorOr<SalesPartyResponse>> UpdateAsync(Guid id, UpdateSalesPartyRequest request, CancellationToken cancellationToken = default);
}

public interface ICashInventoryService
{
    Task<ErrorOr<IReadOnlyList<CashInventoryResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<IReadOnlyList<CashLedgerEntryResponse>>> GetLedgerAsync(Guid? salesPartyId, CancellationToken cancellationToken = default);
    Task<ErrorOr<TicketCashBreakdownResponse>> GetTicketBreakdownAsync(Guid ticketId, CancellationToken cancellationToken = default);
}

public interface ITicketSaleDistributionWriter
{
    Task ApplyAsync(Ticket ticket, decimal ticketFareEtb, CancellationToken cancellationToken = default);
}

public sealed class SalesPartyService(IApplicationDbContext db, IBusinessClock clock) : ISalesPartyService
{
    public async Task<ErrorOr<SalesPartyResponse>> CreateAsync(
        CreateSalesPartyRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        var code = request.Code.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
        {
            return DomainErrors.SalesPartyNameRequired;
        }

        if (!TryParseSource(request.Source, out var source))
        {
            return DomainErrors.InvalidSalesPartySource;
        }

        if (!TryParseAllocationType(request.AllocationType, out var allocationType))
        {
            return DomainErrors.InvalidSalesPartyAllocationType;
        }

        if (allocationType == SalesPartyAllocationType.FixedAmount && request.AmountPerSeatEtb <= 0)
        {
            return DomainErrors.InvalidSalesPartyAmount;
        }

        if (allocationType == SalesPartyAllocationType.BusOwnerRemainder && request.AmountPerSeatEtb != 0)
        {
            return DomainErrors.BusOwnerRemainderMustHaveZeroAmount;
        }

        if (await db.SalesParties.AnyAsync(x => x.Code == code, cancellationToken))
        {
            return DomainErrors.DuplicateSalesPartyCode;
        }

        if (allocationType == SalesPartyAllocationType.BusOwnerRemainder
            && await db.SalesParties.AnyAsync(x => x.AllocationType == SalesPartyAllocationType.BusOwnerRemainder && x.IsActive, cancellationToken))
        {
            return DomainErrors.DuplicateBusOwnerRemainderParty;
        }

        var party = new SalesParty
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            AmountPerSeatEtb = request.AmountPerSeatEtb,
            Source = source,
            AllocationType = allocationType,
            SortOrder = request.SortOrder
        };

        db.SalesParties.Add(party);
        db.CashInventories.Add(new CashInventory { SalesPartyId = party.Id, BalanceEtb = 0 });

        await db.SaveChangesAsync(cancellationToken);
        return Map(party);
    }

    public async Task<ErrorOr<IReadOnlyList<SalesPartyResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var parties = await db.SalesParties.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return parties.Select(Map).ToList();
    }

    public async Task<ErrorOr<SalesPartyResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var party = await db.SalesParties.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return party is null ? DomainErrors.SalesPartyNotFound : Map(party);
    }

    public async Task<ErrorOr<SalesPartyResponse>> UpdateAsync(
        Guid id,
        UpdateSalesPartyRequest request,
        CancellationToken cancellationToken = default)
    {
        var party = await db.SalesParties.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (party is null)
        {
            return DomainErrors.SalesPartyNotFound;
        }

        if (!TryParseSource(request.Source, out var source))
        {
            return DomainErrors.InvalidSalesPartySource;
        }

        if (!TryParseAllocationType(request.AllocationType, out var allocationType))
        {
            return DomainErrors.InvalidSalesPartyAllocationType;
        }

        if (allocationType == SalesPartyAllocationType.FixedAmount && request.AmountPerSeatEtb <= 0)
        {
            return DomainErrors.InvalidSalesPartyAmount;
        }

        if (allocationType == SalesPartyAllocationType.BusOwnerRemainder && request.AmountPerSeatEtb != 0)
        {
            return DomainErrors.BusOwnerRemainderMustHaveZeroAmount;
        }

        if (allocationType == SalesPartyAllocationType.BusOwnerRemainder
            && request.IsActive
            && await db.SalesParties.AnyAsync(
                x => x.Id != id
                     && x.AllocationType == SalesPartyAllocationType.BusOwnerRemainder
                     && x.IsActive,
                cancellationToken))
        {
            return DomainErrors.DuplicateBusOwnerRemainderParty;
        }

        party.Name = request.Name.Trim();
        party.AmountPerSeatEtb = request.AmountPerSeatEtb;
        party.Source = source;
        party.AllocationType = allocationType;
        party.SortOrder = request.SortOrder;
        party.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);
        return Map(party);
    }

    private static bool TryParseSource(string value, out SalesPartySource source) =>
        Enum.TryParse(value, true, out source);

    private static bool TryParseAllocationType(string value, out SalesPartyAllocationType allocationType) =>
        Enum.TryParse(value, true, out allocationType);

    private SalesPartyResponse Map(SalesParty party) =>
        new(
            party.Id,
            party.Name,
            party.Code,
            party.AmountPerSeatEtb,
            party.Source.ToString(),
            party.AllocationType.ToString(),
            party.SortOrder,
            party.IsActive,
            clock.ToLocalDateTime(party.CreatedAt));
}

public sealed class CashInventoryService(IApplicationDbContext db, IBusinessClock clock) : ICashInventoryService
{
    public async Task<ErrorOr<IReadOnlyList<CashInventoryResponse>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await db.CashInventories.AsNoTracking()
            .Include(x => x.SalesParty)
            .OrderBy(x => x.SalesParty.SortOrder)
            .ToListAsync(cancellationToken);

        return items.Select(x => new CashInventoryResponse(
            x.SalesPartyId,
            x.SalesParty.Code,
            x.SalesParty.Name,
            x.SalesParty.Source.ToString(),
            x.BalanceEtb,
            clock.ToLocalDateTime(x.UpdatedAt))).ToList();
    }

    public async Task<ErrorOr<IReadOnlyList<CashLedgerEntryResponse>>> GetLedgerAsync(
        Guid? salesPartyId,
        CancellationToken cancellationToken = default)
    {
        var query = db.CashLedgerEntries.AsNoTracking()
            .Include(x => x.SalesParty)
            .AsQueryable();

        if (salesPartyId.HasValue)
        {
            query = query.Where(x => x.SalesPartyId == salesPartyId.Value);
        }

        var entries = await query
            .OrderByDescending(x => x.OccurredAt)
            .Take(500)
            .ToListAsync(cancellationToken);

        return entries.Select(x => new CashLedgerEntryResponse(
            x.Id,
            x.SalesPartyId,
            x.SalesParty.Code,
            x.SalesParty.Name,
            x.TicketId,
            x.EntryType.ToString(),
            x.AmountEtb,
            x.BalanceAfterEtb,
            clock.ToLocalDateTime(x.OccurredAt))).ToList();
    }

    public async Task<ErrorOr<TicketCashBreakdownResponse>> GetTicketBreakdownAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
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
}

public sealed class TicketSaleDistributionWriter(
    IApplicationDbContext db,
    IBusinessClock clock) : ITicketSaleDistributionWriter
{
    public async Task ApplyAsync(Ticket ticket, decimal ticketFareEtb, CancellationToken cancellationToken = default)
    {
        var parties = await db.SalesParties
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        if (parties.Count == 0)
        {
            return;
        }

        var planned = TicketSaleDistributor.Plan(ticketFareEtb, parties);
        var now = clock.UtcNow;

        foreach (var item in planned)
        {
            var distribution = new TicketSaleDistribution
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                SalesPartyId = item.Party.Id,
                PartyCode = item.Party.Code,
                PartyName = item.Party.Name,
                Source = item.Party.Source,
                AllocationType = item.Party.AllocationType,
                AmountEtb = item.AmountEtb,
                CreatedAt = now
            };

            db.TicketSaleDistributions.Add(distribution);

            var inventory = await db.CashInventories
                .SingleAsync(x => x.SalesPartyId == item.Party.Id, cancellationToken);

            inventory.BalanceEtb += item.AmountEtb;
            inventory.UpdatedAt = now;

            db.CashLedgerEntries.Add(new CashLedgerEntry
            {
                Id = Guid.NewGuid(),
                SalesPartyId = item.Party.Id,
                TicketId = ticket.Id,
                EntryType = CashLedgerEntryType.TicketSaleCredit,
                AmountEtb = item.AmountEtb,
                BalanceAfterEtb = inventory.BalanceEtb,
                OccurredAt = now
            });
        }
    }
}
