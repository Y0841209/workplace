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

        // NOTA: el índice singleton ux_app_settings_singleton se crea via SQL
        // (database/scripts/005_reservations.sql -> ON app_settings ((TRUE))).
        // EF Core no puede representar un índice sobre una constante con
        // HasIndex(lambda), por eso no se declara aqui.
    }
}