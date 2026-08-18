namespace WorkplaceBooking.Domain.Entities;

public enum ReservationStatus
{
    CONFIRMED,
    CHECKED_IN,
    CHECKED_OUT,
    CANCELLED,
    COMPLETED,
    NOT_CHECKED_IN,
    REJECTED
}

public class Reservation : AggregateRoot, IAuditableEntity
{
    public Guid ResourceId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateOnly ReservationDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public ReservationStatus Status { get; private set; }
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public int? AttendeeCount { get; private set; }
    public string? SupportChangeReason { get; private set; }
    public DateTimeOffset? CheckedInAt { get; private set; }
    public DateTimeOffset? CheckedOutAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public Guid? CancelledByUserId { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public Resource? Resource { get; private set; }
    public AppUser? User { get; private set; }
    public AppUser? CreatedByUser { get; private set; }
    public AppUser? CancelledByUser { get; private set; }
    public CheckIn? CheckIn { get; private set; }

    private Reservation() { }

    private Reservation(
        Guid id,
        Guid resourceId,
        Guid userId,
        Guid createdByUserId,
        DateOnly reservationDate,
        TimeOnly startTime,
        TimeOnly endTime,
        string? title,
        string? description,
        int? attendeeCount)
        : base(id)
    {
        ResourceId = resourceId;
        UserId = userId;
        CreatedByUserId = createdByUserId;
        ReservationDate = reservationDate;
        StartTime = startTime;
        EndTime = endTime;
        Status = ReservationStatus.CONFIRMED;
        Title = title;
        Description = description;
        AttendeeCount = attendeeCount;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<Reservation> Create(
        Guid resourceId,
        Guid userId,
        Guid createdByUserId,
        DateOnly reservationDate,
        TimeOnly startTime,
        TimeOnly endTime,
        string? title = null,
        string? description = null,
        int? attendeeCount = null)
    {
        if (resourceId == Guid.Empty)
            return Result.Failure(new Error("RESERVATION_RESOURCE_REQUIRED", "Resource is required"));

        if (userId == Guid.Empty)
            return Result.Failure(new Error("RESERVATION_USER_REQUIRED", "User is required"));

        if (createdByUserId == Guid.Empty)
            return Result.Failure(new Error("RESERVATION_CREATOR_REQUIRED", "Creator is required"));

        if (endTime <= startTime)
            return Result.Failure(new Error("RESERVATION_TIME_ORDER_INVALID", "End time must be after start time"));

        var duration = endTime - startTime;
        if (duration < TimeSpan.FromHours(1))
            return Result.Failure(new Error("RESERVATION_MIN_DURATION", "Reservation must be at least 1 hour"));

        if (endTime > new TimeOnly(23, 59))
            return Result.Failure(new Error("RESERVATION_MAX_END_TIME", "Reservation cannot end after 23:59"));

        if (attendeeCount.HasValue && attendeeCount <= 0)
            return Result.Failure(new Error("RESERVATION_ATTENDEE_COUNT_INVALID", "Attendee count must be positive"));

        var reservation = new Reservation(Guid.NewGuid(), resourceId, userId, createdByUserId, reservationDate, startTime, endTime, title, description, attendeeCount);
        reservation.RaiseDomainEvent(new ReservationCreatedEvent(reservation));
        return Result.Success(reservation);
    }

    public Result Modify(
        DateOnly? reservationDate = null,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        string? title = null,
        string? description = null,
        int? attendeeCount = null,
        string? supportChangeReason = null,
        Guid? modifiedByUserId = null,
        bool isSupportUser = false)
    {
        // Only owner or support can modify
        if (UserId != modifiedByUserId && !isSupportUser)
            return Result.Failure(new Error("RESERVATION_MODIFY_FORBIDDEN", "Only reservation owner or support can modify"));

        // Support must provide reason
        if (isSupportUser && string.IsNullOrWhiteSpace(supportChangeReason))
            return Result.Failure(new Error("RESERVATION_SUPPORT_REASON_REQUIRED", "Support must provide change reason"));

        // Cannot modify completed/cancelled reservations
        if (Status is ReservationStatus.COMPLETED or ReservationStatus.CANCELLED or ReservationStatus.NOT_CHECKED_IN)
            return Result.Failure(new Error("RESERVATION_CANNOT_MODIFY", $"Cannot modify reservation with status {Status}"));

        var newDate = reservationDate ?? ReservationDate;
        var newStart = startTime ?? StartTime;
        var newEnd = endTime ?? EndTime;

        if (newEnd <= newStart)
            return Result.Failure(new Error("RESERVATION_TIME_ORDER_INVALID", "End time must be after start time"));

        var duration = newEnd - newStart;
        if (duration < TimeSpan.FromHours(1))
            return Result.Failure(new Error("RESERVATION_MIN_DURATION", "Reservation must be at least 1 hour"));

        if (newEnd > new TimeOnly(23, 59))
            return Result.Failure(new Error("RESERVATION_MAX_END_TIME", "Reservation cannot end after 23:59"));

        if (attendeeCount.HasValue && attendeeCount <= 0)
            return Result.Failure(new Error("RESERVATION_ATTENDEE_COUNT_INVALID", "Attendee count must be positive"));

        // Apply changes
        if (reservationDate.HasValue) ReservationDate = reservationDate.Value;
        if (startTime.HasValue) StartTime = startTime.Value;
        if (endTime.HasValue) EndTime = endTime.Value;
        if (title != null) Title = title;
        if (description != null) Description = description;
        if (attendeeCount.HasValue) AttendeeCount = attendeeCount.Value;
        if (supportChangeReason != null) SupportChangeReason = supportChangeReason;

        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new ReservationModifiedEvent(this));
        return Result.Success();
    }

    public Result Cancel(Guid cancelledByUserId, string? reason, bool isSupportUser = false)
    {
        if (UserId != cancelledByUserId && !isSupportUser)
            return Result.Failure(new Error("RESERVATION_CANCEL_FORBIDDEN", "Only reservation owner or support can cancel"));

        if (isSupportUser && string.IsNullOrWhiteSpace(reason))
            return Result.Failure(new Error("RESERVATION_SUPPORT_REASON_REQUIRED", "Support must provide cancellation reason"));

        if (Status is ReservationStatus.CANCELLED or ReservationStatus.COMPLETED or ReservationStatus.NOT_CHECKED_IN)
            return Result.Failure(new Error("RESERVATION_CANNOT_CANCEL", $"Cannot cancel reservation with status {Status}"));

        Status = ReservationStatus.CANCELLED;
        CancelledAt = DateTimeOffset.UtcNow;
        CancelledByUserId = cancelledByUserId;
        CancellationReason = reason;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new ReservationCancelledEvent(this));
        return Result.Success();
    }

    public Result CheckIn(Guid checkedInByUserId, string scannedPublicQrId)
    {
        if (UserId != checkedInByUserId)
            return Result.Failure(new Error("CHECKIN_OWNERSHIP_REQUIRED", "Only reservation owner can check in"));

        if (Status != ReservationStatus.CONFIRMED)
            return Result.Failure(new Error("CHECKIN_INVALID_STATUS", $"Cannot check in reservation with status {Status}"));

        // Resource type validation is done in checkin trigger

        Status = ReservationStatus.CHECKED_IN;
        CheckedInAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CheckInCompletedEvent(this));
        return Result.Success();
    }

    public Result CheckOut()
    {
        if (Status != ReservationStatus.CHECKED_IN)
            return Result.Failure(new Error("CHECKOUT_INVALID_STATUS", "Only checked-in reservations can be checked out"));

        Status = ReservationStatus.CHECKED_OUT;
        CheckedOutAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new CheckOutCompletedEvent(this));
        return Result.Success();
    }

    public void AutoComplete()
    {
        if (Status == ReservationStatus.CONFIRMED)
            Status = ReservationStatus.NOT_CHECKED_IN;
        else if (Status == ReservationStatus.CHECKED_IN)
            Status = ReservationStatus.CHECKED_OUT;
        else if (Status == ReservationStatus.CHECKED_OUT)
            Status = ReservationStatus.COMPLETED;

        UpdatedAt = DateTimeOffset.UtcNow;
    }
}