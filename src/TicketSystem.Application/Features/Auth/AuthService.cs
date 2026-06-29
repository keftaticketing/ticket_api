namespace TicketSystem.Application.Features.Auth;

using ErrorOr;
using TicketSystem.Application.Abstractions.Authentication;
using TicketSystem.Application.Errors;
using TicketSystem.Contracts.Auth;

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

    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
}

public sealed class AuthService(
    IIdentityAuthService identityAuth,
    ITokenService tokenService,
    IRefreshTokenService refreshTokenService,
    IIdentityAccountService accountService) : IAuthService
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
        return new LoginResponse(
            tokens.AccessToken,
            tokens.AccessExpiresIn,
            tokens.RefreshToken,
            tokens.RefreshExpiresIn,
            tokens.Role,
            tokens.FullName,
            tokens.MustChangePassword);
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
        return new AuthTokenResponse(
            access.Token,
            access.ExpiresIn,
            rotation.Value.RefreshToken,
            rotation.Value.RefreshExpiresIn,
            PrimaryRole(user.Roles),
            user.FullName,
            user.MustChangePassword);
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

        return new AuthTokenResponse(
            access.Token,
            access.ExpiresIn,
            refresh.RefreshToken,
            refresh.ExpiresIn,
            PrimaryRole(user.Roles),
            user.FullName,
            user.MustChangePassword);
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

    private static string PrimaryRole(IReadOnlyList<string> roles) =>
        roles.Contains(RoleNames.Admin) ? RoleNames.Admin : roles[0];
}

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Ticketer = "Ticketer";
}
