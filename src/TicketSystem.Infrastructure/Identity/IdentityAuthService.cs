namespace TicketSystem.Infrastructure.Identity;

using ErrorOr;
using Microsoft.AspNetCore.Identity;
using TicketSystem.Application.Abstractions.Authentication;
using TicketSystem.Application.Errors;

public sealed class IdentityAuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) : IIdentityAuthService
{
    public async Task<ErrorOr<AuthenticatedUser>> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByNameAsync(username);
        if (user is null || !user.IsActive)
        {
            return DomainErrors.InvalidCredentials;
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        if (!signInResult.Succeeded)
        {
            return DomainErrors.InvalidCredentials;
        }

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count == 0)
        {
            return DomainErrors.InvalidCredentials;
        }

        return new AuthenticatedUser(
            user.Id,
            user.UserName ?? username,
            user.FullName,
            roles.ToList());
    }
}
