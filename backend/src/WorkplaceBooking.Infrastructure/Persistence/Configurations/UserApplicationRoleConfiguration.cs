using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Infrastructure.Persistence.Configurations;

public class UserApplicationRoleConfiguration : IEntityTypeConfiguration<UserApplicationRole>
{
    public void Configure(EntityTypeBuilder<UserApplicationRole> builder)
    {
        builder.ToTable("user_application_roles", "booking");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.RoleCode).IsRequired().HasMaxLength(50);
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

        builder.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AssignedByUser)
            .WithMany()
            .HasForeignKey(x => x.AssignedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_role_dates", "expires_at IS NULL OR expires_at >= valid_from");
        });

        builder.HasIndex(x => new { x.UserId, x.RoleCode })
            .IsUnique()
            .HasFilter("active = true")
            .HasDatabaseName("ux_user_role_active");
    }
}