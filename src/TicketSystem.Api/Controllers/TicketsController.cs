namespace TicketSystem.Api.Controllers;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Extensions;
using TicketSystem.Application.Features.Tickets;
using TicketSystem.Contracts.Tickets;

[ApiController]
[Route("api/tickets")]
public sealed class TicketsController(ITicketService ticketService) : ControllerBase
{
    [Authorize(Roles = "Ticketer")]
    [HttpPost("cash")]
    public async Task<ActionResult<SellCashTicketResponse>> SellCash([FromBody] SellCashTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await ticketService.SellCashAsync(request, GetUserId(), cancellationToken);
        if (result.IsError)
        {
            return result.ToActionResult();
        }

        return result.ToCreatedResult(this, nameof(GetById), value => new { id = value.Ticket.Id });
    }

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await ticketService.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TicketResponse>>> Search(
        [FromQuery] Guid? scheduleId,
        [FromQuery] string? passengerPhone,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var result = await ticketService.SearchAsync(scheduleId, passengerPhone, date, cancellationToken);
        return result.ToActionResult();
    }

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (value is null || !Guid.TryParse(value, out var userId))
        {
            throw new InvalidOperationException("User id claim is missing.");
        }

        return userId;
    }
}
