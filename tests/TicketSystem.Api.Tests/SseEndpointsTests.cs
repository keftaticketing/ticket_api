using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using TicketSystem.Contracts.Tickets;

namespace TicketSystem.Api.Tests;

[Collection("Api")]
public sealed class SseEndpointsTests(TicketSystemWebApplicationFactory factory) : EndpointTestBase(factory)
{
    [Fact]
    public async Task StreamSchedule_WhenMissing_ReturnsNotFound()
    {
        var client = TicketerClient();
        var response = await client.SendAsync(BuildScheduleStreamRequest(Guid.NewGuid()));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StreamSchedule_EmitsConnectedThenSeatSold()
    {
        var scheduleId = await SeedScheduleAsync("AA-40001", 20);
        var listener = Factory.CreateClientWithCredentials("ticketer", TestDataSeeder.TicketerWorkingPassword);
        var seller = TicketerClient();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var request = BuildScheduleStreamRequest(scheduleId);

        var listenerTask = Task.Run(async () =>
        {
            var response = await listener.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            response.EnsureSuccessStatusCode();
            response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");

            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var sawConnected = false;
            var sawSeatSold = false;

            while (!cts.Token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cts.Token);
                if (line is null)
                {
                    break;
                }

                if (line == "event: connected")
                {
                    sawConnected = true;
                }

                if (line == "event: seat.sold")
                {
                    var dataLine = await reader.ReadLineAsync(cts.Token);
                    dataLine.Should().NotBeNull();
                    dataLine!.Should().Contain("\"seatNumber\":7");
                    sawSeatSold = true;
                    break;
                }
            }

            return sawConnected && sawSeatSold;
        }, cts.Token);

        await Task.Delay(500, cts.Token);

        var sellResponse = await seller.PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 7, "SSE Buyer", "0911000077", null));
        sellResponse.EnsureSuccessStatusCode();

        var result = await listenerTask.WaitAsync(TimeSpan.FromSeconds(10));
        result.Should().BeTrue();
    }

    [Fact]
    public async Task StreamSchedule_SurvivesHeartbeatAndSeatSold()
    {
        var scheduleId = await SeedScheduleAsync("AA-40002", 20);
        var listener = Factory.CreateClientWithCredentials("ticketer", TestDataSeeder.TicketerWorkingPassword);
        var seller = TicketerClient();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var request = BuildScheduleStreamRequest(scheduleId);

        var listenerTask = Task.Run(async () =>
        {
            var response = await listener.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var sawHeartbeat = false;
            var sawSeatSold = false;

            while (!cts.Token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cts.Token);
                if (line is null)
                {
                    break;
                }

                if (line == "event: heartbeat")
                {
                    sawHeartbeat = true;
                }

                if (line == "event: seat.sold")
                {
                    sawSeatSold = true;
                    break;
                }
            }

            return sawHeartbeat && sawSeatSold;
        }, cts.Token);

        await Task.Delay(1500, cts.Token);

        var sellResponse = await seller.PostAsJsonAsync("/api/tickets/cash",
            new SellCashTicketRequest(scheduleId, 8, "SSE Heartbeat Buyer", "0911000088", null));
        sellResponse.EnsureSuccessStatusCode();

        var result = await listenerTask.WaitAsync(TimeSpan.FromSeconds(7));
        result.Should().BeTrue();

        // Server should still accept normal requests after SSE activity.
        var health = await TicketerClient().GetAsync($"/api/schedules/{scheduleId}/seats");
        health.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static HttpRequestMessage BuildScheduleStreamRequest(Guid scheduleId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/sse/schedules/{scheduleId}");
        request.Headers.Accept.ParseAdd("text/event-stream");
        return request;
    }
}
