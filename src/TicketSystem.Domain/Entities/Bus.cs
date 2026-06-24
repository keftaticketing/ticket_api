namespace TicketSystem.Domain.Entities;

using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;

public class Bus : IAuditableEntity
{
    public Guid Id { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerPhone { get; set; } = string.Empty;
    public string DelegatePhone { get; set; } = string.Empty;
    public string SideNumber { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public int SeatCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Schedule> Schedules { get; set; } = [];
}
