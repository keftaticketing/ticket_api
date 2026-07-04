namespace TicketSystem.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

public sealed class BusTypeConfiguration : IEntityTypeConfiguration<BusType>
{
    public void Configure(EntityTypeBuilder<BusType> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Code).HasMaxLength(30);
        builder.Property(x => x.Name).HasMaxLength(100);
    }
}
