namespace TicketSystem.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Extensions;
using TicketSystem.Application.Abstractions.Authentication;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Common;
using TicketSystem.Application.Features.Routes;
using TicketSystem.Contracts.Routes;

[ApiController]
[Route("api/routes")]
public sealed class RoutesController(
    IRouteService routeService,
    IIdentityAccountService accountService,
    IBusinessClock clock) : ControllerBase
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
        [FromQuery] Guid? fromStationId,
        CancellationToken cancellationToken)
    {
        var scope = await User.ResolveFromStationFilterAsync(fromStationId, accountService, cancellationToken);
        if (scope.IsError)
        {
            return scope.ToErrorActionResult<Guid?, IReadOnlyList<RouteResponse>>();
        }

        var result = await routeService.GetAllAsync(toCityId, scope.Value, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("seats")]
    public async Task<ActionResult<RouteSeatMapsResponse>> GetSeatMapsByDestination(
        [FromQuery] Guid destinationCityId,
        [FromQuery] string date,
        [FromQuery] Guid? fromStationId,
        CancellationToken cancellationToken)
    {
        var scope = await User.ResolveFromStationFilterAsync(fromStationId, accountService, cancellationToken);
        if (scope.IsError)
        {
            return scope.ToErrorActionResult<Guid?, RouteSeatMapsResponse>();
        }

        var parsedDate = TravelDateParser.ParseLocalDate(date, clock);
        if (parsedDate.IsError)
        {
            return parsedDate.ToErrorActionResult<DateOnly, RouteSeatMapsResponse>();
        }

        var result = await routeService.GetSeatMapsByDestinationAsync(
            destinationCityId,
            parsedDate.Value,
            scope.Value,
            cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RouteResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var scope = await User.ResolveFromStationFilterAsync(null, accountService, cancellationToken);
        if (scope.IsError)
        {
            return scope.ToErrorActionResult<Guid?, RouteResponse>();
        }

        var result = await routeService.GetByIdAsync(id, scope.Value, cancellationToken);
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
