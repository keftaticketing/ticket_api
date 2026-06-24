namespace TicketSystem.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using TicketSystem.Domain.Common;
using TicketSystem.Domain.Entities;
using TicketSystem.Domain.Enums;

public static class SalesPartySeeder
{
    public static readonly Guid OrganizationSalesFeeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb001");
    public static readonly Guid PlatformId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb002");
    public static readonly Guid OrganizationBusLevyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb003");
    public static readonly Guid BusOwnerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbb004");

    public static async Task SeedAsync(TicketSystemDbContext db)
    {
        if (await db.SalesParties.AnyAsync())
        {
            return;
        }

        var parties = new[]
        {
            CreateParty(
                OrganizationSalesFeeId,
                "Organization (Sales Fee)",
                DefaultSalesPartyCodes.OrganizationSalesFee,
                5m,
                SalesPartySource.SalesFee,
                SalesPartyAllocationType.FixedAmount,
                1),
            CreateParty(
                PlatformId,
                "Platform",
                DefaultSalesPartyCodes.Platform,
                12m,
                SalesPartySource.SalesFee,
                SalesPartyAllocationType.FixedAmount,
                2),
            CreateParty(
                OrganizationBusLevyId,
                "Organization (Bus Levy)",
                DefaultSalesPartyCodes.OrganizationBusLevy,
                3m,
                SalesPartySource.BusOwnerIncome,
                SalesPartyAllocationType.FixedAmount,
                3),
            CreateParty(
                BusOwnerId,
                "Bus Owner",
                DefaultSalesPartyCodes.BusOwner,
                0m,
                SalesPartySource.BusOwnerIncome,
                SalesPartyAllocationType.BusOwnerRemainder,
                4)
        };

        foreach (var party in parties)
        {
            db.SalesParties.Add(party);
            db.CashInventories.Add(new CashInventory { SalesPartyId = party.Id, BalanceEtb = 0 });
        }
    }

    private static SalesParty CreateParty(
        Guid id,
        string name,
        string code,
        decimal amount,
        SalesPartySource source,
        SalesPartyAllocationType allocationType,
        int sortOrder) =>
        new()
        {
            Id = id,
            Name = name,
            Code = code,
            AmountPerSeatEtb = amount,
            Source = source,
            AllocationType = allocationType,
            SortOrder = sortOrder
        };
}
