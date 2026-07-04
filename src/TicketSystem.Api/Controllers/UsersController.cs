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

    [HttpGet("{id:guid}/station-assignments")]
    public async Task<ActionResult<IReadOnlyList<UserStationAssignmentSummaryResponse>>> ListStationAssignments(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await accountService.ListStationAssignmentsAsync(id, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/station-assignments")]
    public async Task<ActionResult<UserStationAssignmentSummaryResponse>> AssignStation(
        Guid id,
        [FromBody] CreateUserStationAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await accountService.AssignStationAsync(id, request.StationId, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/station-assignments/{assignmentId:guid}/end")]
    public async Task<ActionResult<UserStationAssignmentSummaryResponse>> EndStationAssignment(
        Guid id,
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var result = await accountService.EndStationAssignmentAsync(id, assignmentId, cancellationToken);
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
