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
        builder.HasIndex(x => new { x.BusLevelId, x.BusTypeId, x.IsActive });
        builder.HasIndex(x => new { x.RouteId, x.BusLevelId, x.BusTypeId, x.IsActive });
        builder.Property(x => x.RatePerKm).HasPrecision(10, 2);
        builder.Property(x => x.Currency).HasMaxLength(10);

        builder.HasOne(x => x.Route)
            .WithMany()
            .HasForeignKey(x => x.RouteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BusLevel)
            .WithMany()
            .HasForeignKey(x => x.BusLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BusType)
            .WithMany()
            .HasForeignKey(x => x.BusTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
