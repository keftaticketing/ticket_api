namespace TicketSystem.Contracts.Auth;

public sealed record LoginRequest(string Username, string Password);

public sealed record StationAssignmentResponse(
    Guid AssignmentId,
    Guid StationId,
    string StationName,
    string StationNameAm,
    string StationCode,
    Guid CityId,
    string CityName,
    bool IsImplicitDefault);

public sealed record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    int RefreshExpiresIn,
    Guid UserId,
    string Username,
    string Role,
    string FullName,
    bool MustChangePassword,
    Guid? DefaultStationId,
    IReadOnlyList<StationAssignmentResponse> StationAssignments);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record LogoutRequest(string RefreshToken);

public sealed record CurrentUserResponse(
    Guid UserId,
    string Username,
    string Role,
    string FullName,
    bool MustChangePassword,
    Guid? DefaultStationId,
    IReadOnlyList<StationAssignmentResponse> StationAssignments);

public sealed record AuthTokenResponse(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    int RefreshExpiresIn,
    Guid UserId,
    string Username,
    string Role,
    string FullName,
    bool MustChangePassword,
    Guid? DefaultStationId,
    IReadOnlyList<StationAssignmentResponse> StationAssignments);
