using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Infrastructure.Persistence.Configurations;

public class UserBusinessProfileConfiguration : IEntityTypeConfiguration<UserBusinessProfile>
{
    public void Configure(EntityTypeBuilder<UserBusinessProfile> builder)
    {
        builder.ToTable("user_business_profiles", "booking");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ProfileCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ValidFrom).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired(false);
        builder.Property(x => x.Active).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.AssignedByUserId).IsRequired(false);
        builder.Property(x => x.AssignmentReason).IsRequired(false).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Profile)
            .WithMany()
            .HasForeignKey(x => x.ProfileCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedByUser)
            .WithMany()
            .HasForeignKey(x => x.AssignedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasCheckConstraint("ck_profile_dates", "expires_at IS NULL OR expires_at >= valid_from");

        builder.HasIndex(x => new { x.UserId, x.ProfileCode })
            .IsUnique()
            .HasFilter("active = true")
            .HasDatabaseName("ux_user_profile_active");
    }
}