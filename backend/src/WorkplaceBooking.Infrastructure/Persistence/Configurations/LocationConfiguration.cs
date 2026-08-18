using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Infrastructure.Persistence.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations", "booking");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.City).IsRequired().HasMaxLength(100).HasDefaultValue("Bogotá");
        builder.Property(x => x.Country).IsRequired().HasMaxLength(100).HasDefaultValue("Colombia");
        builder.Property(x => x.Timezone).IsRequired().HasMaxLength(50).HasDefaultValue("America/Bogota");
        builder.Property(x => x.Active).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_locations_code");
    }
}