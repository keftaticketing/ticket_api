namespace TicketSystem.Domain.Entities;

using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;

public class Schedule : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid RouteId { get; set; }
    public Guid BusId { get; set; }
    public DateTime DepartureAt { get; set; }
    public int SequenceNumber { get; set; }
    public ScheduleStatus Status { get; set; } = ScheduleStatus.Scheduled;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Route Route { get; set; } = null!;
    public Bus Bus { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = [];
}
