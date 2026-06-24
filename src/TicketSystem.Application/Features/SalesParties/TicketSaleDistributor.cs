namespace TicketSystem.Application.Features.SalesParties;

using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

public static class TicketSaleDistributor
{
    public static IReadOnlyList<PlannedDistribution> Plan(
        decimal ticketFareEtb,
        IReadOnlyList<SalesParty> parties)
    {
        var active = parties.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToList();
        if (active.Count == 0)
        {
            return [];
        }

        var remainderParty = active.SingleOrDefault(x => x.AllocationType == SalesPartyAllocationType.BusOwnerRemainder);
        if (remainderParty is null)
        {
            throw new InvalidOperationException("An active bus-owner remainder sales party is required.");
        }

        var fixedDeductions = active
            .Where(x => x.AllocationType == SalesPartyAllocationType.FixedAmount)
            .Sum(x => x.AmountPerSeatEtb);

        var busOwnerNet = ticketFareEtb - fixedDeductions;
        if (busOwnerNet < 0)
        {
            throw new InvalidOperationException("Stakeholder deductions exceed the ticket fare.");
        }

        var planned = new List<PlannedDistribution>();

        foreach (var party in active.Where(x => x.AllocationType == SalesPartyAllocationType.FixedAmount))
        {
            planned.Add(new PlannedDistribution(party, party.AmountPerSeatEtb));
        }

        planned.Add(new PlannedDistribution(remainderParty, busOwnerNet));
        return planned;
    }

    public static decimal GetCommissionFromFareTotal(IReadOnlyList<PlannedDistribution> planned) =>
        planned.Where(x => x.Party.Source == SalesPartySource.SalesFee).Sum(x => x.AmountEtb);
}

public sealed record PlannedDistribution(SalesParty Party, decimal AmountEtb);
