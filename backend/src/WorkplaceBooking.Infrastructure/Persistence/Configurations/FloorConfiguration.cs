using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Infrastructure.Persistence.Configurations;

public class FloorConfiguration : IEntityTypeConfiguration<Floor>
{
    public void Configure(EntityTypeBuilder<Floor> builder)
    {
        builder.ToTable("floors", "booking");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LocationId).IsRequired();
        builder.Property(x => x.FloorNumber).IsRequired();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Active).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.LocationId, x.FloorNumber }).IsUnique().HasDatabaseName("ux_floor_location_number");
        builder.HasIndex(x => new { x.LocationId, x.Code }).IsUnique().HasDatabaseName("ux_floor_location_code");
    }
}