namespace TicketSystem.Domain.Entities;

using TicketSystem.Domain.Enums;

public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? Changes { get; set; }
}
