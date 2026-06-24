namespace TicketSystem.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Extensions;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Common;
using TicketSystem.Application.Features.Routes;
using TicketSystem.Contracts.Routes;

[ApiController]
[Route("api/routes")]
public sealed class RoutesController(IRouteService routeService, IBusinessClock clock) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<RouteResponse>> Create([FromBody] CreateRouteRequest request, CancellationToken cancellationToken)
    {
        var result = await routeService.CreateAsync(request, cancellationToken);
        return result.ToCreatedResult(this, nameof(GetById), value => new { id = value.Id });
    }

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RouteResponse>>> GetAll(
        [FromQuery] Guid? toCityId,
        CancellationToken cancellationToken)
    {
        var result = await routeService.GetAllAsync(toCityId, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("seats")]
    public async Task<ActionResult<RouteSeatMapsResponse>> GetSeatMapsByDestination(
        [FromQuery] Guid destinationCityId,
        [FromQuery] string date,
        CancellationToken cancellationToken)
    {
        var parsedDate = TravelDateParser.ParseLocalDate(date, clock);
        if (parsedDate.IsError)
        {
            return parsedDate.ToErrorActionResult<DateOnly, RouteSeatMapsResponse>();
        }

        var result = await routeService.GetSeatMapsByDestinationAsync(destinationCityId, parsedDate.Value, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RouteResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await routeService.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RouteResponse>> Update(Guid id, [FromBody] UpdateRouteRequest request, CancellationToken cancellationToken)
    {
        var result = await routeService.UpdateAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }
}
