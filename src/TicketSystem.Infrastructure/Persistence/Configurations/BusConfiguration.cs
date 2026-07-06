namespace TicketSystem.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

public sealed class BusConfiguration : IEntityTypeConfiguration<Bus>
{
    public void Configure(EntityTypeBuilder<Bus> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PlateNumber).IsUnique();
        builder.HasIndex(x => x.SideNumber).IsUnique();
        builder.Property(x => x.OwnerName).HasMaxLength(200);
        builder.Property(x => x.OwnerPhone).HasMaxLength(30);
        builder.Property(x => x.DelegatePhone).HasMaxLength(30);
        builder.Property(x => x.SideNumber).HasMaxLength(50);
        builder.Property(x => x.PlateNumber).HasMaxLength(50);

        builder.HasIndex(x => x.AssociationId);
        builder.HasIndex(x => x.BusLevelId);
        builder.HasIndex(x => x.BusTypeId);

        builder.HasOne(x => x.Association)
            .WithMany()
            .HasForeignKey(x => x.AssociationId)
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
