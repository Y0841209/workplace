using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Infrastructure.Persistence.Configurations;

public class CheckInConfiguration : IEntityTypeConfiguration<CheckIn>
{
    public void Configure(EntityTypeBuilder<CheckIn> builder)
    {
        builder.ToTable("checkins", "booking");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReservationId).IsRequired();
        builder.Property(x => x.ResourceId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Method).IsRequired().HasConversion<string>().HasMaxLength(50).HasDefaultValue(CheckInMethod.QR);
        builder.Property(x => x.ScannedPublicQrId).IsRequired();
        builder.Property(x => x.CheckedInAt).IsRequired();
        builder.Property(x => x.IpAddress).IsRequired(false).HasColumnType("inet");
        builder.Property(x => x.UserAgent).IsRequired(false);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.Reservation)
            .WithOne()
            .HasForeignKey<CheckIn>(x => x.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Resource)
            .WithMany()
            .HasForeignKey(x => x.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ReservationId).IsUnique().HasDatabaseName("ux_checkins_reservation");
        builder.HasIndex(x => new { x.UserId, x.CheckedInAt }).HasDatabaseName("ix_checkins_user");
        builder.HasIndex(x => new { x.ResourceId, x.CheckedInAt }).HasDatabaseName("ix_checkins_resource");
    }
}