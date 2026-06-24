namespace TicketSystem.Application.Features.Auth;

using ErrorOr;
using TicketSystem.Application.Abstractions.Authentication;
using TicketSystem.Contracts.Auth;

public interface IAuthService
{
    Task<ErrorOr<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}

public sealed class AuthService(
    IIdentityAuthService identityAuth,
    ITokenService tokenService) : IAuthService
{
    public async Task<ErrorOr<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var authResult = await identityAuth.ValidateCredentialsAsync(request.Username, request.Password, cancellationToken);
        if (authResult.IsError)
        {
            return authResult.Errors;
        }

        var user = authResult.Value;
        var primaryRole = user.Roles.Contains(RoleNames.Admin) ? RoleNames.Admin : user.Roles[0];
        var (token, expiresIn) = tokenService.CreateToken(user.Id, user.Username, user.FullName, user.Roles);

        return new LoginResponse(token, expiresIn, primaryRole, user.FullName);
    }
}

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Ticketer = "Ticketer";
}
