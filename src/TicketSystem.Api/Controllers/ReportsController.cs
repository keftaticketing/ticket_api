namespace TicketSystem.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Extensions;
using TicketSystem.Application.Features.Reports;
using TicketSystem.Contracts.Reports;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public sealed class ReportsController(IReportsService reportsService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardReportResponse>> GetDashboard(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int top = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await reportsService.GetDashboardAsync(from, to, top, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("tickets-by-day")]
    public async Task<ActionResult<IReadOnlyList<DailyTicketStatsResponse>>> GetTicketsByDay(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var result = await reportsService.GetTicketsByDayAsync(from, to, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("top-buses")]
    public async Task<ActionResult<IReadOnlyList<TopBusStatsResponse>>> GetTopBuses(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int top = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await reportsService.GetTopBusesAsync(from, to, top, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("top-counters")]
    public async Task<ActionResult<IReadOnlyList<TopCounterStatsResponse>>> GetTopCounters(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int top = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await reportsService.GetTopCountersAsync(from, to, top, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("revenue-by-party")]
    public async Task<ActionResult<IReadOnlyList<DailyPartyRevenueResponse>>> GetRevenueByParty(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken = default)
    {
        var result = await reportsService.GetRevenueByPartyAsync(from, to, cancellationToken);
        return result.ToActionResult();
    }
}
