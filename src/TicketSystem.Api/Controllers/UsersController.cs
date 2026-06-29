namespace TicketSystem.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Api.Extensions;
using TicketSystem.Application.Abstractions.Authentication;
using TicketSystem.Application.Features.Auth;
using TicketSystem.Contracts.Users;

[ApiController]
[Authorize(Roles = RoleNames.Admin)]
[Route("api/users")]
public sealed class UsersController(IIdentityAccountService accountService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserSummaryResponse>>> List(CancellationToken cancellationToken)
    {
        var result = await accountService.ListUsersAsync(cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<ActionResult<UserSummaryResponse>> CreateTicketer(
        [FromBody] CreateTicketerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountService.CreateTicketerAsync(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}/active")]
    public async Task<ActionResult<UserSummaryResponse>> SetActive(
        Guid id,
        [FromBody] SetUserActiveRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountService.SetUserActiveAsync(id, request.IsActive, cancellationToken);
        return result.ToActionResult();
    }
}
