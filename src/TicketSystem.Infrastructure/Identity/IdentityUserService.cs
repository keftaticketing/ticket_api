namespace TicketSystem.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using TicketSystem.Application.Abstractions.Authentication;

public sealed class IdentityUserService(UserManager<ApplicationUser> userManager) : IIdentityUserService
{
    public async Task<bool> IsActiveTicketerAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return false;
        }

        return await userManager.IsInRoleAsync(user, RoleNames.Ticketer);
    }

    public async Task<string?> GetFullNameAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user?.FullName;
    }
}
