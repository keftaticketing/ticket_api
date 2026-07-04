namespace TicketSystem.Contracts.Users;

public sealed record CreateTicketerRequest(
    string Username,
    string FullName,
    string Password,
    string? Email = null);

public sealed record UserSummaryResponse(
    Guid Id,
    string Username,
    string FullName,
    string Role,
    bool IsActive,
    bool MustChangePassword);

public sealed record UserStationAssignmentSummaryResponse(
    Guid AssignmentId,
    Guid UserId,
    Guid StationId,
    string StationName,
    string StationNameAm,
    string StationCode,
    Guid CityId,
    string CityName,
    bool IsImplicitDefault,
    DateTime AssignedAtUtc,
    DateTime? EndedAtUtc,
    bool IsActive);

public sealed record CreateUserStationAssignmentRequest(Guid StationId);

public sealed record SetUserActiveRequest(bool IsActive);
