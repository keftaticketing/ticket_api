namespace TicketSystem.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Extensions;
using TicketSystem.Application.Features.Tariffs;
using TicketSystem.Contracts.Tariffs;

[ApiController]
[Route("api/tariffs")]
public sealed class TariffsController(ITariffService tariffService) : ControllerBase
{
    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("active")]
    public async Task<ActionResult<TariffResponse>> GetActive(CancellationToken cancellationToken)
    {
        var result = await tariffService.GetActiveAsync(cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut]
    public async Task<ActionResult<TariffResponse>> SetActive([FromBody] SetTariffRequest request, CancellationToken cancellationToken)
    {
        var result = await tariffService.SetActiveAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<TariffResponse>>> GetHistory(CancellationToken cancellationToken)
    {
        var result = await tariffService.GetHistoryAsync(cancellationToken);
        return result.ToActionResult();
    }
}
