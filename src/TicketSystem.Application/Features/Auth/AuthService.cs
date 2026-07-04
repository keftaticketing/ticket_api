namespace TicketSystem.Application.Features.Auth;

using ErrorOr;
using Abstractions.Authentication;
using Abstractions.Persistence;
using TicketSystem.Contracts.Auth;
using Microsoft.EntityFrameworkCore;

public interface IAuthService
{
    Task<ErrorOr<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<ErrorOr<AuthTokenResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<AuthTokenResponse>> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<CurrentUserResponse>> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);

    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
}

public sealed class AuthService(
    IIdentityAuthService identityAuth,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService,
    IIdentityAccountService accountService,
    IApplicationDbContext dbContext) : IAuthService
{
    public async Task<ErrorOr<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var authResult = await identityAuth.ValidateCredentialsAsync(
            request.Username,
            request.Password,
            cancellationToken);
        if (authResult.IsError)
        {
            return authResult.Errors;
        }

        var tokens = await IssueTokensAsync(authResult.Value, cancellationToken);
        var stationContext = await GetStationContextAsync(authResult.Value.Id, cancellationToken);
        return new LoginResponse(
            tokens.AccessToken,
            tokens.AccessExpiresIn,
            tokens.RefreshToken,
            tokens.RefreshExpiresIn,
            authResult.Value.Id,
            authResult.Value.Username,
            tokens.Role,
            tokens.FullName,
            tokens.MustChangePassword,
            stationContext.DefaultStationId,
            stationContext.Assignments);
    }

    public async Task<ErrorOr<AuthTokenResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var rotation = await refreshTokenService.ValidateAndRotateAsync(
            request.RefreshToken,
            cancellationToken);
        if (rotation.IsError)
        {
            return rotation.Errors;
        }

        var user = rotation.Value.User;
        var access = CreateAccess(user);
        var stationContext = await GetStationContextAsync(user.Id, cancellationToken);
        return new AuthTokenResponse(
            access.Token,
            access.ExpiresIn,
            rotation.Value.RefreshToken,
            rotation.Value.RefreshExpiresIn,
            user.Id,
            user.Username,
            PrimaryRole(user.Roles),
            user.FullName,
            user.MustChangePassword,
            stationContext.DefaultStationId,
            stationContext.Assignments);
    }

    public async Task<ErrorOr<AuthTokenResponse>> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var changeResult = await accountService.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);
        if (changeResult.IsError)
        {
            return changeResult.Errors;
        }

        var user = changeResult.Value;
        var access = CreateAccess(user);
        var refresh = await refreshTokenService.IssueAsync(user.Id, cancellationToken);
        var stationContext = await GetStationContextAsync(user.Id, cancellationToken);

        return new AuthTokenResponse(
            access.Token,
            access.ExpiresIn,
            refresh.RefreshToken,
            refresh.ExpiresIn,
            user.Id,
            user.Username,
            PrimaryRole(user.Roles),
            user.FullName,
            user.MustChangePassword,
            stationContext.DefaultStationId,
            stationContext.Assignments);
    }

    public async Task<ErrorOr<CurrentUserResponse>> GetMeAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var userResult = await accountService.GetAuthenticatedUserAsync(userId, cancellationToken);
        if (userResult.IsError)
        {
            return userResult.Errors;
        }

        var user = userResult.Value;
        var stationContext = await GetStationContextAsync(user.Id, cancellationToken);

        return new CurrentUserResponse(
            user.Id,
            user.Username,
            PrimaryRole(user.Roles),
            user.FullName,
            user.MustChangePassword,
            stationContext.DefaultStationId,
            stationContext.Assignments);
    }

    public Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default) =>
        refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);

    private async Task<AuthTokens> IssueTokensAsync(
        AuthenticatedUser user,
        CancellationToken cancellationToken)
    {
        var access = CreateAccess(user);
        var refresh = await refreshTokenService.IssueAsync(user.Id, cancellationToken);

        return new AuthTokens(
            access.Token,
            access.ExpiresIn,
            refresh.RefreshToken,
            refresh.ExpiresIn,
            PrimaryRole(user.Roles),
            user.FullName,
            user.MustChangePassword);
    }

    private (string Token, int ExpiresIn) CreateAccess(AuthenticatedUser user) =>
        tokenService.CreateAccessToken(
            user.Id,
            user.Username,
            user.FullName,
            user.Roles,
            user.MustChangePassword);

    private async Task<StationContext> GetStationContextAsync(Guid userId, CancellationToken cancellationToken)
    {
        var assignments = await dbContext.UserStationAssignments
            .AsNoTracking()
            .Include(x => x.Station)
            .ThenInclude(x => x.City)
            .Where(x => x.UserId == userId && x.EndedAtUtc == null)
            .OrderBy(x => x.AssignedAtUtc)
            .Select(x => new StationAssignmentResponse(
                x.Id,
                x.StationId,
                x.Station.Name,
                x.Station.NameAm,
                x.Station.Code,
                x.Station.CityId,
                x.Station.City.Name,
                x.Station.IsImplicitDefault))
            .ToListAsync(cancellationToken);

        Guid? defaultStationId = assignments.Count == 1
            ? assignments[0].StationId
            : null;

        return new StationContext(assignments, defaultStationId);
    }

    private static string PrimaryRole(IReadOnlyList<string> roles) =>
        roles.Contains(RoleNames.Admin) ? RoleNames.Admin : roles[0];

    private sealed record StationContext(
        IReadOnlyList<StationAssignmentResponse> Assignments,
        Guid? DefaultStationId);
}

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Ticketer = "Ticketer";
}
