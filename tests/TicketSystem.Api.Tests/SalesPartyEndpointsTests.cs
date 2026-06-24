using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TicketSystem.Contracts.SalesParties;
using TicketSystem.Contracts.Tickets;
using TicketSystem.Domain.Common;

namespace TicketSystem.Api.Tests;

[Collection("Api")]
public sealed class SalesPartyEndpointsTests(TicketSystemWebApplicationFactory factory) : EndpointTestBase(factory)
{
    [Fact]
    public async Task GetSalesParties_AsAdmin_ReturnsSeededParties()
    {
        var response = await AdminClient().GetAsync("/api/sales-parties");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<SalesPartyResponse>>();
        body.Should().Contain(x => x.Code == DefaultSalesPartyCodes.OrganizationSalesFee && x.AmountPerSeatEtb == 5m);
        body.Should().Contain(x => x.Code == DefaultSalesPartyCodes.Platform && x.AmountPerSeatEtb == 12m);
        body.Should().Contain(x => x.Code == DefaultSalesPartyCodes.OrganizationBusLevy && x.AmountPerSeatEtb == 3m);
    }

    [Fact]
    public async Task CreateSalesParty_AsTicketer_ReturnsForbidden()
    {
        var response = await TicketerClient().PostAsJsonAsync("/api/sales-parties",
            new CreateSalesPartyRequest("Agent", "AGENT", 2m, "SalesFee", "FixedAmount", 5));
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCashInventory_AsAdmin_ReturnsZeroBalancesInitially()
    {
        var response = await AdminClient().GetAsync("/api/cash-inventory");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CashInventoryResponse>>();
        body.Should().NotBeEmpty();
        body!.Should().OnlyContain(x => x.BalanceEtb == 0);
    }
}

[Collection("Api")]
public sealed class TicketCashDistributionTests(TicketSystemWebApplicationFactory factory) : EndpointTestBase(factory)
{
    [Fact]
    public async Task SellCashTicket_DistributesCashToSalesParties()
    {
        var scheduleId = await SeedScheduleAsync("AA-40001", 20);
        var client = TicketerClient();
        var response = await client.PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 4, "Cash Split Test", "0911004400", null));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<SellCashTicketResponse>();
        body!.CashBreakdown.Should().NotBeNull();
        body.CashBreakdown!.TicketFareEtb.Should().Be(346 * 2.50m);
        body.CashBreakdown.SalesFeeTotalEtb.Should().Be(17m);
        body.CashBreakdown.TotalCashCollectedEtb.Should().Be(body.CashBreakdown.TicketFareEtb);

        body.CashBreakdown.Distributions.Should().Contain(x =>
            x.PartyCode == DefaultSalesPartyCodes.OrganizationSalesFee && x.AmountEtb == 5m);
        body.CashBreakdown.Distributions.Should().Contain(x =>
            x.PartyCode == DefaultSalesPartyCodes.Platform && x.AmountEtb == 12m);
        body.CashBreakdown.Distributions.Should().Contain(x =>
            x.PartyCode == DefaultSalesPartyCodes.OrganizationBusLevy && x.AmountEtb == 3m);
        body.CashBreakdown.Distributions.Should().Contain(x =>
            x.PartyCode == DefaultSalesPartyCodes.BusOwner
            && x.AmountEtb == body.CashBreakdown.TicketFareEtb - 20m);

        var inventory = await AdminClient().GetAsync("/api/cash-inventory");
        var balances = await inventory.Content.ReadFromJsonAsync<List<CashInventoryResponse>>();
        balances!.Single(x => x.PartyCode == DefaultSalesPartyCodes.Platform).BalanceEtb.Should().Be(12m);
        balances.Single(x => x.PartyCode == DefaultSalesPartyCodes.OrganizationSalesFee).BalanceEtb.Should().Be(5m);
    }
}
