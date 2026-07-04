namespace TicketSystem.Application.Abstractions.Authentication;

using ErrorOr;
using TicketSystem.Contracts.Users;

public interface IIdentityAccountService
{
    Task<ErrorOr<AuthenticatedUser>> GetAuthenticatedUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<AuthenticatedUser>> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<UserSummaryResponse>> CreateTicketerAsync(
        CreateTicketerRequest request,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<IReadOnlyList<UserSummaryResponse>>> ListUsersAsync(
        CancellationToken cancellationToken = default);

    Task<ErrorOr<UserSummaryResponse>> SetUserActiveAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default);
}
