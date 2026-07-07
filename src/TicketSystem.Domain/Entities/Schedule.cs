namespace TicketSystem.Domain.Entities;

using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;

public class Schedule : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid RouteId { get; set; }
    public Guid BusId { get; set; }
    public DateTime DepartureAt { get; set; }
    public DateOnly DepartureDate { get; set; }
    public int SequenceNumber { get; set; }
    public Guid AssociationId { get; set; }
    public Guid BusLevelId { get; set; }
    public Guid BusTypeId { get; set; }
    public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Route Route { get; set; } = null!;
    public Bus Bus { get; set; } = null!;
    public Association Association { get; set; } = null!;
    public BusLevel BusLevel { get; set; } = null!;
    public BusType BusType { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = [];
}
