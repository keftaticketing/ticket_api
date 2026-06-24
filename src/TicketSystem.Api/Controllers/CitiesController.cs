namespace TicketSystem.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Extensions;
using TicketSystem.Application.Features.Cities;
using TicketSystem.Contracts.Cities;

[ApiController]
[Route("api/cities")]
public sealed class CitiesController(ICityService cityService) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CityResponse>> Create([FromBody] CreateCityRequest request, CancellationToken cancellationToken)
    {
        var result = await cityService.CreateAsync(request, cancellationToken);
        if (result.IsError)
        {
            return result.ToActionResult();
        }

        return result.ToCreatedResult($"/api/cities/{result.Value.Id}");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CityResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await cityService.GetAllAsync(cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("destinations")]
    public async Task<ActionResult<IReadOnlyList<CityResponse>>> GetDestinations(CancellationToken cancellationToken)
    {
        var result = await cityService.GetDestinationsAsync(cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<CityResponse>>> Search(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var result = await cityService.SearchAsync(q, cancellationToken);
        return result.ToActionResult();
    }
}
