namespace TicketSystem.Api.Extensions;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Application.Abstractions.Authentication;
using TicketSystem.Application.Features.Auth;

public static class TicketerSellingScopeExtensions
{
    public static async Task<ErrorOr<Guid?>> ResolveFromStationFilterAsync(
        this ClaimsPrincipal user,
        Guid? requestedFromStationId,
        IIdentityAccountService accountService,
        CancellationToken cancellationToken = default)
    {
        var isAdmin = user.IsInRole(RoleNames.Admin);
        Guid? userId = null;
        if (!isAdmin)
        {
            var value = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (value is null || !Guid.TryParse(value, out var parsedUserId))
            {
                return Error.Unauthorized("Auth.UserIdMissing", "User id claim is missing.");
            }

            userId = parsedUserId;
        }

        return await TicketerSellingScope.ResolveFromStationFilterAsync(
            isAdmin,
            userId,
            requestedFromStationId,
            accountService,
            cancellationToken);
    }
}
