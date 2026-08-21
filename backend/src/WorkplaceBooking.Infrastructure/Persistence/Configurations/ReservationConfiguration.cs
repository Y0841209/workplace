using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("reservations", "booking");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ResourceId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.ReservationDate).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Title).IsRequired(false).HasMaxLength(200);
        builder.Property(x => x.Description).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.AttendeeCount).IsRequired(false);
        builder.Property(x => x.SupportChangeReason).IsRequired(false).HasMaxLength(500);
        builder.Property(x => x.CheckedInAt).IsRequired(false);
        builder.Property(x => x.CheckedOutAt).IsRequired(false);
        builder.Property(x => x.CancelledAt).IsRequired(false);
        builder.Property(x => x.CancelledByUserId).IsRequired(false);
        builder.Property(x => x.CancellationReason).IsRequired(false).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasOne(x => x.Resource)
            .WithMany()
            .HasForeignKey(x => x.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CancelledByUser)
            .WithMany()
            .HasForeignKey(x => x.CancelledByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Check constraints
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_reservation_time_order", "end_time > start_time");
            t.HasCheckConstraint("ck_reservation_min_duration",
                "(reservation_date + end_time) - (reservation_date + start_time) >= INTERVAL '1 hour'");
            t.HasCheckConstraint("ck_reservation_latest_end_time", "end_time <= TIME '23:59'");
            t.HasCheckConstraint("ck_attendee_count", "attendee_count IS NULL OR attendee_count > 0");
        });

        // Exclusion constraints for preventing double booking (requires btree_gist extension)
        // These are created via raw SQL in migration
        builder.HasIndex(x => new { x.UserId, x.ReservationDate }).HasDatabaseName("ix_reservations_user_date");
        builder.HasIndex(x => new { x.ResourceId, x.ReservationDate }).HasDatabaseName("ix_reservations_resource_date");
        builder.HasIndex(x => x.Status).HasDatabaseName("ix_reservations_status");
        builder.HasIndex(x => new { x.UserId, x.ReservationDate, x.Status })
            .HasFilter("status IN ('CONFIRMED', 'CHECKED_IN')")
            .HasDatabaseName("ix_reservations_future_active");
    }
}