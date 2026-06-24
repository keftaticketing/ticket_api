namespace TicketSystem.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Extensions;
using TicketSystem.Application.Features.SalesParties;
using TicketSystem.Contracts.SalesParties;

[ApiController]
[Route("api/sales-parties")]
public sealed class SalesPartiesController(ISalesPartyService salesPartyService) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<SalesPartyResponse>> Create(
        [FromBody] CreateSalesPartyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await salesPartyService.CreateAsync(request, cancellationToken);
        return result.ToCreatedResult(this, nameof(GetById), value => new { id = value.Id });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SalesPartyResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await salesPartyService.GetAllAsync(cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesPartyResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await salesPartyService.GetByIdAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SalesPartyResponse>> Update(
        Guid id,
        [FromBody] UpdateSalesPartyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await salesPartyService.UpdateAsync(id, request, cancellationToken);
        return result.ToActionResult();
    }
}

[ApiController]
[Route("api/cash-inventory")]
public sealed class CashInventoryController(ICashInventoryService cashInventoryService) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CashInventoryResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await cashInventoryService.GetAllAsync(cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("ledger")]
    public async Task<ActionResult<IReadOnlyList<CashLedgerEntryResponse>>> GetLedger(
        [FromQuery] Guid? salesPartyId,
        CancellationToken cancellationToken)
    {
        var result = await cashInventoryService.GetLedgerAsync(salesPartyId, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("tickets/{ticketId:guid}")]
    public async Task<ActionResult<TicketCashBreakdownResponse>> GetTicketBreakdown(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var result = await cashInventoryService.GetTicketBreakdownAsync(ticketId, cancellationToken);
        return result.ToActionResult();
    }
}
