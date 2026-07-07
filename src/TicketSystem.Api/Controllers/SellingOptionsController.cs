namespace TicketSystem.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Extensions;
using TicketSystem.Application.Abstractions.Authentication;
using TicketSystem.Application.Abstractions.Time;
using TicketSystem.Application.Common;
using TicketSystem.Application.Features.SellingOptions;
using TicketSystem.Contracts.SellingOptions;

[ApiController]
[Route("api/selling-options")]
public sealed class SellingOptionsController(
    ISellingOptionService sellingOptionService,
    IIdentityAccountService accountService,
    IBusinessClock clock) : ControllerBase
{
    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<SellingOptionSummaryResponse>>> Search(
        [FromQuery] Guid toCityId,
        [FromQuery] string date,
        [FromQuery] Guid? fromStationId,
        CancellationToken cancellationToken)
    {
        var scope = await User.ResolveFromStationFilterAsync(fromStationId, accountService, cancellationToken);
        if (scope.IsError)
        {
            return scope.ToErrorActionResult<Guid?, IReadOnlyList<SellingOptionSummaryResponse>>();
        }

        var parsedDate = TravelDateParser.ParseLocalDate(date, clock);
        if (parsedDate.IsError)
        {
            return parsedDate.ToErrorActionResult<DateOnly, IReadOnlyList<SellingOptionSummaryResponse>>();
        }

        var result = await sellingOptionService.SearchAsync(
            toCityId,
            parsedDate.Value,
            scope.Value,
            cancellationToken);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin,Ticketer")]
    [HttpGet("{optionKey}/schedules")]
    public async Task<ActionResult<IReadOnlyList<SellingOptionScheduleResponse>>> GetSchedules(
        string optionKey,
        [FromQuery] Guid? fromStationId,
        CancellationToken cancellationToken)
    {
        var scope = await User.ResolveFromStationFilterAsync(fromStationId, accountService, cancellationToken);
        if (scope.IsError)
        {
            return scope.ToErrorActionResult<Guid?, IReadOnlyList<SellingOptionScheduleResponse>>();
        }

        var result = await sellingOptionService.GetSchedulesAsync(optionKey, scope.Value, cancellationToken);
        return result.ToActionResult();
    }
}
