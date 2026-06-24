namespace TicketSystem.Domain.Entities;

using TicketSystem.Domain.Enums;

public class TicketSaleDistribution
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public Guid SalesPartyId { get; set; }
    public SalesParty SalesParty { get; set; } = null!;
    public string PartyCode { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public SalesPartySource Source { get; set; }
    public SalesPartyAllocationType AllocationType { get; set; }
    public decimal AmountEtb { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
