namespace TicketSystem.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Extensions;
using TicketSystem.Application.Features.Buses;
using TicketSystem.Contracts.Buses;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/buses")]
public sealed class BusesController(IBusService busService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BusResponse>> Create([FromBody] CreateBusRequest request, CancellationToken cancellationToken)
    {
        var result = await busService.CreateAsync(request, cancellationToken);
        return result.ToCreatedResult(this, nameof(GetById), value => new { id = value.Id });
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BusResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await busService.GetAllAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BusResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await busService.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BusResponse>> Update(Guid id, [FromBody] UpdateBusRequest request, CancellationToken cancellationToken)
    {
        var result = await busService.UpdateAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }
}
