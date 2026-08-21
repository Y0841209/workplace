using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Infrastructure.Persistence.Configurations;

public class ReservationExceptionConfiguration : IEntityTypeConfiguration<ReservationException>
{
    public void Configure(EntityTypeBuilder<ReservationException> builder)
    {
        builder.ToTable("reservation_exceptions", "booking");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.MaximumFutureActiveReservations).IsRequired();
        builder.Property(x => x.AppliesToResourceTypeCode).IsRequired(false).HasMaxLength(50);
        builder.Property(x => x.ValidFrom).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Active).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AppliesToResourceType)
            .WithMany()
            .HasForeignKey(x => x.AppliesToResourceTypeCode)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_exception_limit", "maximum_future_active_reservations > 0");
            t.HasCheckConstraint("ck_exception_dates", "expires_at >= valid_from");
        });
    }
}