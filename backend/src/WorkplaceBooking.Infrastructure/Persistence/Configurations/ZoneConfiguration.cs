using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Infrastructure.Persistence.Configurations;

public class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> builder)
    {
        builder.ToTable("zones", "booking");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FloorId).IsRequired();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Active).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasOne(x => x.Floor)
            .WithMany()
            .HasForeignKey(x => x.FloorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.FloorId, x.Code }).IsUnique().HasDatabaseName("ux_zone_floor_code");
    }
}