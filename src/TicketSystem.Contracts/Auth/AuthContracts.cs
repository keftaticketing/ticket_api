namespace TicketSystem.Contracts.Auth;

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    int RefreshExpiresIn,
    string Role,
    string FullName,
    bool MustChangePassword);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthTokenResponse(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    int RefreshExpiresIn,
    string Role,
    string FullName,
    bool MustChangePassword);
