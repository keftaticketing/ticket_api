namespace TicketSystem.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

public sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.ScheduleId, x.SeatNumber }).IsUnique();
        builder.Property(x => x.PassengerName).HasMaxLength(200);
        builder.Property(x => x.PassengerPhone).HasMaxLength(30);
        builder.Property(x => x.NationalId).HasMaxLength(50);
        builder.Property(x => x.SoldByUserName).HasMaxLength(200);
        builder.Property(x => x.Price).HasPrecision(12, 2);
        builder.Property(x => x.DistanceKm).HasPrecision(10, 2);
        builder.Property(x => x.RatePerKm).HasPrecision(10, 2);
        builder.HasOne(x => x.Schedule).WithMany(x => x.Tickets).HasForeignKey(x => x.ScheduleId);
    }
}
