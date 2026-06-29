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

public sealed record SetUserActiveRequest(bool IsActive);
