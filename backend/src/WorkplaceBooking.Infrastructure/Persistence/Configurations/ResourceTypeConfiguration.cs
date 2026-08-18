using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Infrastructure.Persistence.Configurations;

public class ResourceTypeConfiguration : IEntityTypeConfiguration<ResourceType>
{
    public void Configure(EntityTypeBuilder<ResourceType> builder)
    {
        builder.ToTable("resource_types", "booking");
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.QrRequired).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.CheckinRequired).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.Active).IsRequired().HasDefaultValue(true);
    }
}