namespace TicketSystem.Application.Abstractions.Authentication;

using ErrorOr;

public sealed record AuthenticatedUser(
    Guid Id,
    string Username,
    string FullName,
    IReadOnlyList<string> Roles);

public interface IIdentityAuthService
{
    Task<ErrorOr<AuthenticatedUser>> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}
