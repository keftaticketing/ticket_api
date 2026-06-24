namespace TicketSystem.Application.Abstractions.Authentication;

public interface IIdentityUserService
{
    Task<bool> IsActiveTicketerAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<string?> GetFullNameAsync(Guid userId, CancellationToken cancellationToken = default);
}
