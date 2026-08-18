using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Infrastructure.Persistence.Configurations;

public class BusinessProfileConfiguration : IEntityTypeConfiguration<BusinessProfile>
{
    public void Configure(EntityTypeBuilder<BusinessProfile> builder)
    {
        builder.ToTable("business_profiles", "booking");
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Active).IsRequired().HasDefaultValue(true);
    }
}