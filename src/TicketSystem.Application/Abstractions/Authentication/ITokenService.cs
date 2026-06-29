namespace TicketSystem.Application.Abstractions.Authentication;

using ErrorOr;

public interface ITokenService
{
    (string Token, int ExpiresIn) CreateAccessToken(
        Guid userId,
        string username,
        string fullName,
        IReadOnlyList<string> roles,
        bool mustChangePassword);
}

public interface IRefreshTokenService
{
    Task<(string RefreshToken, int ExpiresIn)> IssueAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<RefreshRotationResult>> ValidateAndRotateAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
}
