namespace TicketSystem.Infrastructure.Identity;

using ErrorOr;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Application.Abstractions.Authentication;
using TicketSystem.Application.Errors;
using TicketSystem.Application.Features.Auth;
using TicketSystem.Contracts.Users;
using TicketSystem.Infrastructure.Persistence;

public sealed class IdentityAccountService(
    UserManager<ApplicationUser> userManager,
    TicketSystemDbContext dbContext) : IIdentityAccountService
{
    public async Task<ErrorOr<AuthenticatedUser>> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return DomainErrors.UserNotFound;
        }

        var changeResult = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!changeResult.Succeeded)
        {
            if (changeResult.Errors.Any(x => x.Code == "PasswordMismatch"))
            {
                return DomainErrors.CurrentPasswordIncorrect;
            }

            var message = string.Join(' ', changeResult.Errors.Select(x => x.Description));
            return DomainErrors.PasswordChangeFailed(message);
        }

        user.MustChangePassword = false;
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        return new AuthenticatedUser(
            user.Id,
            user.UserName ?? string.Empty,
            user.FullName,
            roles.ToList(),
            user.MustChangePassword);
    }

    public async Task<ErrorOr<UserSummaryResponse>> CreateTicketerAsync(
        CreateTicketerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return DomainErrors.UsernameRequired;
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return DomainErrors.FullNameRequired;
        }

        var username = request.Username.Trim();
        if (await userManager.FindByNameAsync(username) is not null)
        {
            return DomainErrors.DuplicateUsername;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = username,
            FullName = request.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email)
                ? $"{username}@ticketsystem.local"
                : request.Email.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            MustChangePassword = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            var message = string.Join(' ', createResult.Errors.Select(x => x.Description));
            return DomainErrors.PasswordChangeFailed(message);
        }

        await userManager.AddToRoleAsync(user, RoleNames.Ticketer);
        return MapSummary(user, RoleNames.Ticketer);
    }

    public async Task<ErrorOr<IReadOnlyList<UserSummaryResponse>>> ListUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users
            .OrderBy(x => x.UserName)
            .ToListAsync(cancellationToken);

        var summaries = new List<UserSummaryResponse>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Count == 0)
            {
                continue;
            }

            summaries.Add(MapSummary(user, PrimaryRole(roles)));
        }

        return summaries;
    }

    public async Task<ErrorOr<UserSummaryResponse>> SetUserActiveAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return DomainErrors.UserNotFound;
        }

        user.IsActive = isActive;
        await userManager.UpdateAsync(user);

        if (!isActive)
        {
            await RevokeRefreshTokensAsync(userId, cancellationToken);
        }

        var roles = await userManager.GetRolesAsync(user);
        return MapSummary(user, PrimaryRole(roles));
    }

    private async Task RevokeRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
        }

        if (tokens.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static UserSummaryResponse MapSummary(ApplicationUser user, string role) =>
        new(
            user.Id,
            user.UserName ?? string.Empty,
            user.FullName,
            role,
            user.IsActive,
            user.MustChangePassword);

    private static string PrimaryRole(IList<string> roles) =>
        roles.Contains(RoleNames.Admin) ? RoleNames.Admin : roles[0];
}
