namespace TicketSystem.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

public sealed class TariffConfiguration : IEntityTypeConfiguration<Tariff>
{
    public void Configure(EntityTypeBuilder<Tariff> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.IsActive);
        builder.Property(x => x.RatePerKm).HasPrecision(10, 2);
        builder.Property(x => x.Currency).HasMaxLength(10);
    }
}
