using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Infrastructure.Persistence.Configurations;

public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("resources", "booking");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ResourceTypeCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.LocationId).IsRequired();
        builder.Property(x => x.FloorId).IsRequired();
        builder.Property(x => x.ZoneId).IsRequired(false);
        builder.Property(x => x.Capacity).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.PublicQrId).IsRequired(false);
        builder.Property(x => x.QrVersion).IsRequired().HasDefaultValue(1);
        builder.Property(x => x.Active).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.Reservable).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        // QR Policy Check Constraint
        builder.HasCheckConstraint("ck_resource_qr_policy",
            "(resource_type_code IN ('OPEN_WORKSPACE','CLOSED_OFFICE') AND public_qr_id IS NOT NULL) " +
            "OR (resource_type_code = 'MEETING_ROOM' AND public_qr_id IS NULL)");

        // Capacity check constraint
        builder.HasCheckConstraint("ck_resource_capacity", "capacity > 0");

        builder.HasOne(x => x.ResourceType)
            .WithMany()
            .HasForeignKey(x => x.ResourceTypeCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Location)
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Floor)
            .WithMany()
            .HasForeignKey(x => x.FloorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Zone)
            .WithMany()
            .HasForeignKey(x => x.ZoneId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ux_resources_code");
        builder.HasIndex(x => x.ResourceTypeCode).HasDatabaseName("ix_resources_type");
        builder.HasIndex(x => x.FloorId).HasDatabaseName("ix_resources_floor");
        builder.HasIndex(x => new { x.Active, x.Reservable }).HasDatabaseName("ix_resources_active_reservable");
        builder.HasIndex(x => x.PublicQrId).IsUnique().HasDatabaseName("ix_resources_public_qr");
    }
}