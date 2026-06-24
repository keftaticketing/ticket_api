using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TicketSystem.Contracts.Reports;
using TicketSystem.Contracts.Tickets;
using TicketSystem.Domain.Common;

namespace TicketSystem.Api.Tests;

[Collection("Api")]
public sealed class ReportEndpointsTests(TicketSystemWebApplicationFactory factory) : EndpointTestBase(factory)
{
    [Fact]
    public async Task GetDashboard_AsAdmin_ReturnsAggregatedStats()
    {
        var scheduleA = await SeedScheduleAsync("AA-50001", 20, "Jimma");
        var scheduleB = await SeedScheduleAsync("AA-50002", 20, "Hawassa");
        var ticketer = TicketerClient();

        (await ticketer.PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleA, 1, "Passenger A", "0911005001", null))).EnsureSuccessStatusCode();
        (await ticketer.PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleA, 2, "Passenger B", "0911005002", null))).EnsureSuccessStatusCode();
        (await ticketer.PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleB, 1, "Passenger C", "0911005003", null))).EnsureSuccessStatusCode();

        var today = AddisTestTimes.DateOf(AddisTestTimes.TodayAt(12));
        var response = await AdminClient().GetAsync(
            $"/api/reports/dashboard?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DashboardReportResponse>();
        body.Should().NotBeNull();
        body!.Summary.TotalTicketsSold.Should().Be(3);
        body.Summary.TotalSalesFeeEtb.Should().Be(51m);
        body.Summary.TotalCashCollectedEtb.Should().Be(865m * 2 + 275m * 2.50m);
        body.TicketsByDay.Should().ContainSingle(x => x.TicketCount == 3);
        body.TopBuses.Should().HaveCountGreaterThan(0);
        body.TopBuses.First().TicketsSold.Should().BeGreaterThanOrEqualTo(2);
        body.TopCounters.Should().ContainSingle(x => x.TicketsSold == 3);
        body.Summary.PartyTotals.Should().Contain(x =>
            x.PartyCode == DefaultSalesPartyCodes.Platform && x.AmountEtb == 36m);
    }

    [Fact]
    public async Task GetTicketsByDay_AsAdmin_ReturnsDailyCounts()
    {
        var scheduleId = await SeedScheduleAsync("AA-50003", 15);
        await TicketerClient().PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 1, "Daily Test", "0911005004", null));

        var today = AddisTestTimes.DateOf(AddisTestTimes.TodayAt(12));
        var response = await AdminClient().GetAsync(
            $"/api/reports/tickets-by-day?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<DailyTicketStatsResponse>>();
        body.Should().ContainSingle(x =>
            x.TicketCount == 1
            && x.SalesFeeEtb == 17m
            && x.TotalCashCollectedEtb == x.TicketFareEtb);
    }

    [Fact]
    public async Task GetRevenueByParty_AsAdmin_ReturnsPartyBreakdownByDay()
    {
        var scheduleId = await SeedScheduleAsync("AA-50004", 15);
        await TicketerClient().PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 1, "Party Test", "0911005005", null));

        var today = AddisTestTimes.DateOf(AddisTestTimes.TodayAt(12));
        var response = await AdminClient().GetAsync(
            $"/api/reports/revenue-by-party?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<DailyPartyRevenueResponse>>();
        var day = body!.Single();
        day.Parties.Should().Contain(x =>
            x.PartyCode == DefaultSalesPartyCodes.OrganizationSalesFee && x.AmountEtb == 5m);
        day.Parties.Should().Contain(x =>
            x.PartyCode == DefaultSalesPartyCodes.Platform && x.AmountEtb == 12m);
    }

    [Fact]
    public async Task GetDashboard_AsTicketer_ReturnsForbidden()
    {
        var response = await TicketerClient().GetAsync("/api/reports/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDashboard_WithInvalidDateRange_ReturnsBadRequest()
    {
        var today = AddisTestTimes.DateOf(AddisTestTimes.TodayAt(12));
        var response = await AdminClient().GetAsync(
            $"/api/reports/dashboard?from={today.AddDays(1):yyyy-MM-dd}&to={today:yyyy-MM-dd}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ApiResponses_SerializeDecimalsWithTwoPlaces()
    {
        var scheduleId = await SeedScheduleAsync("AA-50005", 15);
        await TicketerClient().PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 1, "Decimal Test", "0911005006", null));

        var today = AddisTestTimes.DateOf(AddisTestTimes.TodayAt(12));
        var response = await AdminClient().GetAsync(
            $"/api/reports/tickets-by-day?from={today:yyyy-MM-dd}&to={today:yyyy-MM-dd}");

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var fare = document.RootElement[0].GetProperty("ticketFareEtb").GetDecimal();
        fare.Should().Be(865.00m);

        var serialized = document.RootElement[0].GetProperty("ticketFareEtb").GetRawText();
        serialized.Should().MatchRegex(@"^\d+\.\d{2}$");
    }
}
