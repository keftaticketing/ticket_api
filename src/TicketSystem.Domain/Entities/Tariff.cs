namespace TicketSystem.Domain.Entities;

using TicketSystem.Domain.Common;

public class Tariff : IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid? RouteId { get; set; }
    public Guid BusLevelId { get; set; }
    public Guid BusTypeId { get; set; }
    public decimal RatePerKm { get; set; }
    public string Currency { get; set; } = "ETB";
    public bool IsActive { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public Route? Route { get; set; }
    public BusLevel BusLevel { get; set; } = null!;
    public BusType BusType { get; set; } = null!;
}
