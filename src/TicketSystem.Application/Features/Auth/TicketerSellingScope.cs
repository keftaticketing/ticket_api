namespace TicketSystem.Application.Features.Auth;

using ErrorOr;
using TicketSystem.Application.Abstractions.Authentication;
using TicketSystem.Application.Errors;

public static class TicketerSellingScope
{
    public static async Task<ErrorOr<Guid?>> ResolveFromStationFilterAsync(
        bool isAdmin,
        Guid? ticketerId,
        Guid? requestedFromStationId,
        IIdentityAccountService accountService,
        CancellationToken cancellationToken = default)
    {
        if (isAdmin)
        {
            return requestedFromStationId;
        }

        if (ticketerId is null)
        {
            return DomainErrors.TicketerRequired;
        }

        var sellingStationResult = await accountService.ResolveSellingStationIdAsync(
            ticketerId.Value,
            cancellationToken);
        if (sellingStationResult.IsError)
        {
            return sellingStationResult.Errors;
        }

        if (requestedFromStationId is Guid requestedFromStation
            && requestedFromStation != sellingStationResult.Value)
        {
            return DomainErrors.FromStationScopeMismatch;
        }

        return sellingStationResult.Value;
    }

    public static ErrorOr<Success> EnsureRouteOriginMatches(
        Guid routeFromStationId,
        Guid? enforcedFromStationId)
    {
        if (enforcedFromStationId is Guid enforcedFromStation
            && routeFromStationId != enforcedFromStation)
        {
            return DomainErrors.FromStationScopeMismatch;
        }

        return Result.Success;
    }
}
