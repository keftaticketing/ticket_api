namespace TicketSystem.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

public sealed class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.RouteId, x.DepartureAt, x.SequenceNumber }).IsUnique();
        builder.HasOne(x => x.Route).WithMany(x => x.Schedules).HasForeignKey(x => x.RouteId);
        builder.HasOne(x => x.Bus).WithMany(x => x.Schedules).HasForeignKey(x => x.BusId);
    }
}
