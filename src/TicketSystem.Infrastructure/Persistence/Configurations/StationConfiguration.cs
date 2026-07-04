namespace TicketSystem.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

public sealed class StationConfiguration : IEntityTypeConfiguration<Station>
{
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => new { x.CityId, x.Name }).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(100);
        builder.Property(x => x.NameAm).HasMaxLength(100);
        builder.Property(x => x.Code).HasMaxLength(50);

        builder.HasOne(x => x.City)
            .WithMany(x => x.Stations)
            .HasForeignKey(x => x.CityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
