namespace TicketSystem.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketSystem.Domain.Entities;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.EntityName);
        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => x.UserId);
        builder.Property(x => x.EntityName).HasMaxLength(100);
        builder.Property(x => x.EntityId).HasMaxLength(100);
        builder.Property(x => x.UserName).HasMaxLength(256);
        builder.Property(x => x.Changes);
    }
}
