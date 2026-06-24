namespace TicketSystem.Domain.Entities;

public class CashInventory
{
    public Guid SalesPartyId { get; set; }
    public SalesParty SalesParty { get; set; } = null!;
    public decimal BalanceEtb { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
