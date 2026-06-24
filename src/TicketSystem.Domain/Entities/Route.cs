namespace TicketSystem.Domain.Entities;

using TicketSystem.Domain.Common;

public class Route : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid FromCityId { get; set; }
    public City FromCity { get; set; } = null!;
    public Guid ToCityId { get; set; }
    public City ToCity { get; set; } = null!;
    public decimal DistanceKm { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Schedule> Schedules { get; set; } = [];
}
