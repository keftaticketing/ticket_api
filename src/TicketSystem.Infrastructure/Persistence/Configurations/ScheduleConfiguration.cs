namespace TicketSystem.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

public sealed class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new
        {
            x.RouteId,
            x.DepartureDate,
            x.AssociationId,
            x.BusLevelId,
            x.BusTypeId,
            x.SequenceNumber
        });
        builder.HasOne(x => x.Route).WithMany(x => x.Schedules).HasForeignKey(x => x.RouteId);
        builder.HasOne(x => x.Bus).WithMany(x => x.Schedules).HasForeignKey(x => x.BusId);
        builder.HasOne(x => x.Association).WithMany().HasForeignKey(x => x.AssociationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.BusLevel).WithMany().HasForeignKey(x => x.BusLevelId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.BusType).WithMany().HasForeignKey(x => x.BusTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Tariff).WithMany().HasForeignKey(x => x.TariffId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
