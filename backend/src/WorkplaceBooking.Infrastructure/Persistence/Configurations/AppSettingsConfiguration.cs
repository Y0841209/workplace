using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Infrastructure.Persistence.Configurations;

public class AppSettingsConfiguration : IEntityTypeConfiguration<AppSettings>
{
    public void Configure(EntityTypeBuilder<AppSettings> builder)
    {
        builder.ToTable("app_settings", "booking");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MaximumFutureActiveReservations).IsRequired();
        builder.Property(x => x.MaximumAdvanceDays).IsRequired(false);
        builder.Property(x => x.MinimumDurationMinutes).IsRequired();
        builder.Property(x => x.LatestEndTime).IsRequired();
        builder.Property(x => x.ReminderMinutesBefore).IsRequired();
        builder.Property(x => x.AllowCrossDayBooking).IsRequired();
        builder.Property(x => x.ShowOccupantNameToUsers).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => true).IsUnique().HasDatabaseName("ux_app_settings_singleton");
    }
}