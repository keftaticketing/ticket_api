namespace TicketSystem.Application.Abstractions.Authentication;

using ErrorOr;

public sealed record AuthenticatedUser(
    Guid Id,
    string Username,
    string FullName,
    IReadOnlyList<string> Roles,
    bool MustChangePassword);

public sealed record AuthTokens(
    string AccessToken,
    int AccessExpiresIn,
    string RefreshToken,
    int RefreshExpiresIn,
    string Role,
    string FullName,
    bool MustChangePassword);

public sealed record RefreshRotationResult(
    AuthenticatedUser User,
    string RefreshToken,
    int RefreshExpiresIn);

public interface IIdentityAuthService
{
    Task<ErrorOr<AuthenticatedUser>> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}
