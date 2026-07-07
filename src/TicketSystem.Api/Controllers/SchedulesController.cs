namespace TicketSystem.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Extensions;
using TicketSystem.Application.Abstractions.Authentication;
using TicketSystem.Application.Features.Schedules;
using TicketSystem.Contracts.Schedules;

[ApiController]
[Route("api/schedules")]
public sealed class SchedulesController(
    IScheduleService scheduleService,
    IIdentityAccountService accountService) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ScheduleResponse>> Create([FromBody] CreateScheduleRequest request, CancellationToken cancellationToken)
    {
        var result = await scheduleService.CreateAsync(request, cancellationToken);
        if (result.IsError)
        {
            return result.ToActionResult();
        }

        return result.ToCreatedResult($"/api/schedules/{result.Value.Id}");
    }

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScheduleResponse>>> GetAll(
        [FromQuery] Guid? routeId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var scope = await User.ResolveFromStationFilterAsync(null, accountService, cancellationToken);
        if (scope.IsError)
        {
            return scope.ToErrorActionResult<Guid?, IReadOnlyList<ScheduleResponse>>();
        }

        var result = await scheduleService.GetAllAsync(routeId, date, scope.Value, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyList<ScheduleResponse>>> GetAvailable(
        [FromQuery] Guid routeId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var scope = await User.ResolveFromStationFilterAsync(null, accountService, cancellationToken);
        if (scope.IsError)
        {
            return scope.ToErrorActionResult<Guid?, IReadOnlyList<ScheduleResponse>>();
        }

        var result = await scheduleService.GetAvailableAsync(routeId, date, scope.Value, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ScheduleResponse>> Update(Guid id, [FromBody] UpdateScheduleRequest request, CancellationToken cancellationToken)
    {
        var result = await scheduleService.UpdateAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("{id:guid}/seats")]
    public async Task<ActionResult<SeatMapResponse>> GetSeats(Guid id, CancellationToken cancellationToken)
    {
        var scope = await User.ResolveFromStationFilterAsync(null, accountService, cancellationToken);
        if (scope.IsError)
        {
            return scope.ToErrorActionResult<Guid?, SeatMapResponse>();
        }

        var result = await scheduleService.GetSeatMapAsync(id, scope.Value, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("{id:guid}/seats/{seatNumber:int}")]
    public async Task<ActionResult<SeatStatusResponse>> GetSeatStatus(
        Guid id,
        int seatNumber,
        CancellationToken cancellationToken)
    {
        var scope = await User.ResolveFromStationFilterAsync(null, accountService, cancellationToken);
        if (scope.IsError)
        {
            return scope.ToErrorActionResult<Guid?, SeatStatusResponse>();
        }

        var result = await scheduleService.GetSeatStatusAsync(id, seatNumber, scope.Value, cancellationToken);
        return result.ToActionResult();
    }
}
