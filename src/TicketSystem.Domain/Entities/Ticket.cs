namespace TicketSystem.Domain.Entities;

using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;

public class Ticket : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public int SeatNumber { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string PassengerPhone { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public Guid FromCityId { get; set; }
    public string FromCityName { get; set; } = string.Empty;
    public Guid FromStationId { get; set; }
    public string FromStationName { get; set; } = string.Empty;
    public Guid ToCityId { get; set; }
    public string ToCityName { get; set; } = string.Empty;
    public Guid ToStationId { get; set; }
    public string ToStationName { get; set; } = string.Empty;
    public Guid AssociationId { get; set; }
    public string AssociationName { get; set; } = string.Empty;
    public Guid BusLevelId { get; set; }
    public string BusLevelName { get; set; } = string.Empty;
    public Guid BusTypeId { get; set; }
    public string BusTypeName { get; set; } = string.Empty;
    public Guid TariffId { get; set; }
    public decimal Price { get; set; }
    public decimal DistanceKm { get; set; }
    public decimal RatePerKm { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public Guid SoldByUserId { get; set; }
    public string SoldByUserName { get; set; } = string.Empty;
    public DateTime SoldAt { get; set; } = DateTime.UtcNow;

    public Schedule Schedule { get; set; } = null!;
}
