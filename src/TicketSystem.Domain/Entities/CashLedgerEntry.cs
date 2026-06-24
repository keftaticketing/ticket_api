namespace TicketSystem.Domain.Entities;

using TicketSystem.Domain.Enums;

public class CashLedgerEntry
{
    public Guid Id { get; set; }
    public Guid SalesPartyId { get; set; }
    public SalesParty SalesParty { get; set; } = null!;
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public CashLedgerEntryType EntryType { get; set; }
    public decimal AmountEtb { get; set; }
    public decimal BalanceAfterEtb { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
