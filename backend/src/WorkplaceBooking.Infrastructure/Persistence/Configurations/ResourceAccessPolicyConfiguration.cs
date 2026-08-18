using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Infrastructure.Persistence.Configurations;

public class ResourceAccessPolicyConfiguration : IEntityTypeConfiguration<ResourceAccessPolicy>
{
    public void Configure(EntityTypeBuilder<ResourceAccessPolicy> builder)
    {
        builder.ToTable("resource_access_policies", "booking");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ResourceTypeCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.BusinessProfileCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.CanView).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CanReserve).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CanModifyOwn).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.Active).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasOne(x => x.ResourceType)
            .WithMany()
            .HasForeignKey(x => x.ResourceTypeCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BusinessProfile)
            .WithMany()
            .HasForeignKey(x => x.BusinessProfileCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ResourceTypeCode, x.BusinessProfileCode })
            .IsUnique()
            .HasDatabaseName("ux_resource_access_policy");
    }
}