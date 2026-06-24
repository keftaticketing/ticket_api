namespace TicketSystem.Contracts.Auth;

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    string Role,
    string FullName);
