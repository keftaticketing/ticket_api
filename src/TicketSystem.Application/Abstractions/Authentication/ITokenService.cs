namespace TicketSystem.Application.Abstractions.Authentication;

public interface ITokenService
{
    (string Token, int ExpiresIn) CreateToken(
        Guid userId,
        string username,
        string fullName,
        IReadOnlyList<string> roles);
}
