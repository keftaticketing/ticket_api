namespace TicketSystem.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;
using Identity;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.FullName).HasMaxLength(200);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.MustChangePassword).HasDefaultValue(false);

        builder.HasOne<Station>()
            .WithMany()
            .HasForeignKey(x => x.SelectedStationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
