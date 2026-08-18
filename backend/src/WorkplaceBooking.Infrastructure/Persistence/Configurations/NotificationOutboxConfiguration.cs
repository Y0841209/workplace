using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Infrastructure.Persistence.Configurations;

public class NotificationOutboxConfiguration : IEntityTypeConfiguration<NotificationOutbox>
{
    public void Configure(EntityTypeBuilder<NotificationOutbox> builder)
    {
        builder.ToTable("notification_outbox", "booking");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReservationId).IsRequired(false);
        builder.Property(x => x.RecipientUserId).IsRequired();
        builder.Property(x => x.RecipientEmail).IsRequired().HasMaxLength(255).HasColumnType("citext");
        builder.Property(x => x.Type).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Subject).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Body).IsRequired().HasColumnType("text");
        builder.Property(x => x.ScheduledAt).IsRequired();
        builder.Property(x => x.SentAt).IsRequired(false);
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.RetryCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.LastError).IsRequired(false).HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasOne(x => x.Reservation)
            .WithMany()
            .HasForeignKey(x => x.ReservationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.RecipientUser)
            .WithMany()
            .HasForeignKey(x => x.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasCheckConstraint("ck_notification_retry", "retry_count >= 0");

        builder.HasIndex(x => new { x.Status, x.ScheduledAt })
            .HasFilter("status = 'PENDING'")
            .HasDatabaseName("ix_notification_pending");
    }
}