namespace TicketSystem.Domain.Entities;

using TicketSystem.Domain.Common;

public class City : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DistanceFromAddisKm { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Route> RoutesFrom { get; set; } = [];
    public ICollection<Route> RoutesTo { get; set; } = [];
}
