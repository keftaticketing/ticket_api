namespace TicketSystem.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

public sealed class SalesPartyConfiguration : IEntityTypeConfiguration<SalesParty>
{
    public void Configure(EntityTypeBuilder<SalesParty> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.Property(x => x.Code).HasMaxLength(50);
        builder.Property(x => x.AmountPerSeatEtb).HasPrecision(10, 2);
    }
}

public sealed class TicketSaleDistributionConfiguration : IEntityTypeConfiguration<TicketSaleDistribution>
{
    public void Configure(EntityTypeBuilder<TicketSaleDistribution> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.TicketId);
        builder.Property(x => x.PartyCode).HasMaxLength(50);
        builder.Property(x => x.PartyName).HasMaxLength(100);
        builder.Property(x => x.AmountEtb).HasPrecision(10, 2);

        builder.HasOne(x => x.Ticket)
            .WithMany()
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SalesParty)
            .WithMany(x => x.Distributions)
            .HasForeignKey(x => x.SalesPartyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CashInventoryConfiguration : IEntityTypeConfiguration<CashInventory>
{
    public void Configure(EntityTypeBuilder<CashInventory> builder)
    {
        builder.HasKey(x => x.SalesPartyId);
        builder.Property(x => x.BalanceEtb).HasPrecision(14, 2);

        builder.HasOne(x => x.SalesParty)
            .WithOne(x => x.CashInventory)
            .HasForeignKey<CashInventory>(x => x.SalesPartyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CashLedgerEntryConfiguration : IEntityTypeConfiguration<CashLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CashLedgerEntry> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.SalesPartyId, x.OccurredAt });
        builder.HasIndex(x => x.TicketId);
        builder.Property(x => x.AmountEtb).HasPrecision(10, 2);
        builder.Property(x => x.BalanceAfterEtb).HasPrecision(14, 2);

        builder.HasOne(x => x.SalesParty)
            .WithMany(x => x.LedgerEntries)
            .HasForeignKey(x => x.SalesPartyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Ticket)
            .WithMany()
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
