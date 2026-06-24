namespace TicketSystem.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

public sealed class RouteConfiguration : IEntityTypeConfiguration<Route>
{
    public void Configure(EntityTypeBuilder<Route> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.FromCityId, x.ToCityId }).IsUnique();
        builder.Property(x => x.DistanceKm).HasPrecision(10, 2);

        builder.HasOne(x => x.FromCity)
            .WithMany(x => x.RoutesFrom)
            .HasForeignKey(x => x.FromCityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToCity)
            .WithMany(x => x.RoutesTo)
            .HasForeignKey(x => x.ToCityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
