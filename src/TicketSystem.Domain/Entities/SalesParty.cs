namespace TicketSystem.Domain.Entities;

using TicketSystem.Domain.Common;
using TicketSystem.Domain.Enums;

public class SalesParty : IAuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public decimal AmountPerSeatEtb { get; set; }
    public SalesPartySource Source { get; set; }
    public SalesPartyAllocationType AllocationType { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TicketSaleDistribution> Distributions { get; set; } = [];
    public CashInventory? CashInventory { get; set; }
    public ICollection<CashLedgerEntry> LedgerEntries { get; set; } = [];
}
