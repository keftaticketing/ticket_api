namespace TicketSystem.Domain.Entities;

using TicketSystem.Domain.Common;

public class Station : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid CityId { get; set; }
    public City City { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string NameAm { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsImplicitDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
