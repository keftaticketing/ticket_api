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
    public async Task<ErrorOr<AuthenticatedUser>> GetAuthenticatedUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return DomainErrors.UserNotFound;
        }

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count == 0)
        {
            return DomainErrors.UserNotFound;
        }

        return MapAuthenticatedUser(user, roles);
    }

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
        return MapAuthenticatedUser(user, roles);
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

    public async Task<ErrorOr<IReadOnlyList<UserStationAssignmentSummaryResponse>>> ListStationAssignmentsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!await userManager.Users.AnyAsync(x => x.Id == userId, cancellationToken))
        {
            return DomainErrors.UserNotFound;
        }

        var assignments = await dbContext.UserStationAssignments
            .AsNoTracking()
            .Include(x => x.Station)
            .ThenInclude(x => x.City)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.AssignedAtUtc)
            .ToListAsync(cancellationToken);

        return assignments.Select(MapStationAssignment).ToList();
    }

    public async Task<ErrorOr<UserStationAssignmentSummaryResponse>> AssignStationAsync(
        Guid userId,
        Guid stationId,
        CancellationToken cancellationToken = default)
    {
        if (!await userManager.Users.AnyAsync(x => x.Id == userId, cancellationToken))
        {
            return DomainErrors.UserNotFound;
        }

        var station = await dbContext.Stations
            .AsNoTracking()
            .Include(x => x.City)
            .SingleOrDefaultAsync(x => x.Id == stationId && x.IsActive, cancellationToken);
        if (station is null)
        {
            return DomainErrors.StationNotFound;
        }

        var existingActive = await dbContext.UserStationAssignments
            .AsNoTracking()
            .AnyAsync(
                x => x.UserId == userId
                    && x.StationId == stationId
                    && x.EndedAtUtc == null,
                cancellationToken);
        if (existingActive)
        {
            return DomainErrors.StationAssignmentAlreadyActive;
        }

        var assignment = new Domain.Entities.UserStationAssignment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            StationId = stationId,
            AssignedAtUtc = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.UserStationAssignments.Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new UserStationAssignmentSummaryResponse(
            assignment.Id,
            userId,
            station.Id,
            station.Name,
            station.NameAm,
            station.Code,
            station.CityId,
            station.City.Name,
            station.IsImplicitDefault,
            assignment.AssignedAtUtc,
            assignment.EndedAtUtc,
            true);
    }

    public async Task<ErrorOr<UserStationAssignmentSummaryResponse>> EndStationAssignmentAsync(
        Guid userId,
        Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var assignment = await dbContext.UserStationAssignments
            .Include(x => x.Station)
            .ThenInclude(x => x.City)
            .SingleOrDefaultAsync(x => x.Id == assignmentId && x.UserId == userId, cancellationToken);
        if (assignment is null)
        {
            return DomainErrors.StationAssignmentNotFound;
        }

        if (assignment.EndedAtUtc is null)
        {
            assignment.EndedAtUtc = DateTime.UtcNow;
            var user = await userManager.Users
                .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
            if (user is not null && user.SelectedStationId == assignment.StationId)
            {
                user.SelectedStationId = null;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapStationAssignment(assignment);
    }

    public async Task<ErrorOr<AuthenticatedUser>> SetSelectedStationAsync(
        Guid userId,
        Guid? stationId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return DomainErrors.UserNotFound;
        }

        if (stationId is not null)
        {
            var station = await dbContext.Stations
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == stationId.Value && x.IsActive, cancellationToken);
            if (station is null)
            {
                return DomainErrors.StationNotFound;
            }

            var hasActiveAssignment = await dbContext.UserStationAssignments
                .AsNoTracking()
                .AnyAsync(
                    x => x.UserId == userId
                        && x.StationId == stationId.Value
                        && x.EndedAtUtc == null,
                    cancellationToken);
            if (!hasActiveAssignment)
            {
                return DomainErrors.SelectedStationNotAssigned;
            }
        }

        user.SelectedStationId = stationId;
        await dbContext.SaveChangesAsync(cancellationToken);

        var roles = await userManager.GetRolesAsync(user);
        return MapAuthenticatedUser(user, roles);
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

    private static AuthenticatedUser MapAuthenticatedUser(ApplicationUser user, IList<string> roles) =>
        new(
            user.Id,
            user.UserName ?? string.Empty,
            user.FullName,
            roles.ToList(),
            user.MustChangePassword,
            user.SelectedStationId);

    private static UserStationAssignmentSummaryResponse MapStationAssignment(Domain.Entities.UserStationAssignment assignment) =>
        new(
            assignment.Id,
            assignment.UserId,
            assignment.StationId,
            assignment.Station.Name,
            assignment.Station.NameAm,
            assignment.Station.Code,
            assignment.Station.CityId,
            assignment.Station.City.Name,
            assignment.Station.IsImplicitDefault,
            assignment.AssignedAtUtc,
            assignment.EndedAtUtc,
            assignment.EndedAtUtc is null);

    private static string PrimaryRole(IList<string> roles) =>
        roles.Contains(RoleNames.Admin) ? RoleNames.Admin : roles[0];
}
