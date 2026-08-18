namespace WorkplaceBooking.Domain.Entities;

public class AppSettings : Entity, IAuditableEntity
{
    public int MaximumFutureActiveReservations { get; private set; }
    public int? MaximumAdvanceDays { get; private set; }
    public int MinimumDurationMinutes { get; private set; }
    public TimeOnly LatestEndTime { get; private set; }
    public int ReminderMinutesBefore { get; private set; }
    public bool AllowCrossDayBooking { get; private set; }
    public bool ShowOccupantNameToUsers { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private AppSettings() { }

    private AppSettings(
        int maximumFutureActiveReservations,
        int? maximumAdvanceDays,
        int minimumDurationMinutes,
        TimeOnly latestEndTime,
        int reminderMinutesBefore,
        bool allowCrossDayBooking,
        bool showOccupantNameToUsers)
        : base(Guid.NewGuid())
    {
        MaximumFutureActiveReservations = maximumFutureActiveReservations;
        MaximumAdvanceDays = maximumAdvanceDays;
        MinimumDurationMinutes = minimumDurationMinutes;
        LatestEndTime = latestEndTime;
        ReminderMinutesBefore = reminderMinutesBefore;
        AllowCrossDayBooking = allowCrossDayBooking;
        ShowOccupantNameToUsers = showOccupantNameToUsers;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<AppSettings> Create(
        int maximumFutureActiveReservations,
        int? maximumAdvanceDays,
        int minimumDurationMinutes,
        TimeOnly latestEndTime,
        int reminderMinutesBefore,
        bool allowCrossDayBooking,
        bool showOccupantNameToUsers)
    {
        if (maximumFutureActiveReservations <= 0)
            return Result.Failure(new Error("APP_SETTINGS_INVALID_LIMIT", "Maximum future active reservations must be positive"));

        if (minimumDurationMinutes < 60)
            return Result.Failure(new Error("APP_SETTINGS_INVALID_DURATION", "Minimum duration must be at least 60 minutes"));

        if (reminderMinutesBefore < 0)
            return Result.Failure(new Error("APP_SETTINGS_INVALID_REMINDER", "Reminder minutes cannot be negative"));

        return Result.Success(new AppSettings(
            maximumFutureActiveReservations,
            maximumAdvanceDays,
            minimumDurationMinutes,
            latestEndTime,
            reminderMinutesBefore,
            allowCrossDayBooking,
            showOccupantNameToUsers));
    }

    public void Update(
        int? maximumFutureActiveReservations = null,
        int? maximumAdvanceDays = null,
        int? minimumDurationMinutes = null,
        TimeOnly? latestEndTime = null,
        int? reminderMinutesBefore = null,
        bool? allowCrossDayBooking = null,
        bool? showOccupantNameToUsers = null)
    {
        if (maximumFutureActiveReservations.HasValue)
        {
            if (maximumFutureActiveReservations <= 0)
                throw new DomainException("Maximum future active reservations must be positive", "APP_SETTINGS_INVALID_LIMIT");
            MaximumFutureActiveReservations = maximumFutureActiveReservations.Value;
        }

        if (maximumAdvanceDays.HasValue)
            MaximumAdvanceDays = maximumAdvanceDays;

        if (minimumDurationMinutes.HasValue)
        {
            if (minimumDurationMinutes < 60)
                throw new DomainException("Minimum duration must be at least 60 minutes", "APP_SETTINGS_INVALID_DURATION");
            MinimumDurationMinutes = minimumDurationMinutes.Value;
        }

        if (latestEndTime.HasValue)
            LatestEndTime = latestEndTime.Value;

        if (reminderMinutesBefore.HasValue)
        {
            if (reminderMinutesBefore < 0)
                throw new DomainException("Reminder minutes cannot be negative", "APP_SETTINGS_INVALID_REMINDER");
            ReminderMinutesBefore = reminderMinutesBefore.Value;
        }

        if (allowCrossDayBooking.HasValue)
            AllowCrossDayBooking = allowCrossDayBooking.Value;

        if (showOccupantNameToUsers.HasValue)
            ShowOccupantNameToUsers = showOccupantNameToUsers.Value;

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}