using Ardalis.Result;
using Ardalis.Specification;
using BookingPlatform.Domain.Events;
using BookingPlatform.Domain.ValueObjects;
using BookingPlatform.Domain.Enums;

namespace BookingPlatform.Domain.Entities;

public class Reservation : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid ResourceId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateOnly ReservationDate { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public ReservationStatus Status { get; private set; } = ReservationStatus.CONFIRMED;
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public int? AttendeeCount { get; private set; }
    public string? SupportChangeReason { get; private set; }
    public DateTimeOffset? CheckedInAt { get; private set; }
    public DateTimeOffset? CheckedOutAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public Guid? CancelledByUserId { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Resource? Resource { get; private set; }
    public AppUser? User { get; private set; }
    public AppUser? CreatedByUser { get; private set; }
    public AppUser? CancelledByUser { get; private set; }

    private Reservation() { } // EF Core

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
        var errors = new List<string>();

        // Validate time order
        if (endTime <= startTime)
        {
            errors.Add("End time must be after start time");
        }

        // Validate minimum duration (1 hour)
        var duration = endTime - startTime;
        if (duration < TimeSpan.FromHours(1))
        {
            errors.Add("Reservation must be at least 1 hour");
        }

        // Validate max end time
        if (endTime > new TimeOnly(23, 59))
        {
            errors.Add("Reservation cannot end after 23:59");
        }

        // Validate attendee count
        if (attendeeCount.HasValue && attendeeCount <= 0)
        {
            errors.Add("Attendee count must be positive");
        }

        if (errors.Count > 0)
        {
            return Result.Invalid(errors);
        }

        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            UserId = userId,
            CreatedByUserId = createdByUserId,
            ReservationDate = reservationDate,
            StartTime = startTime,
            EndTime = endTime,
            Title = title?.Trim(),
            Description = description?.Trim(),
            AttendeeCount = attendeeCount,
            Status = ReservationStatus.CONFIRMED,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return Result.Success(reservation);
    }

    public Result Modify(
        DateOnly? reservationDate,
        TimeOnly? startTime,
        TimeOnly? endTime,
        string? title,
        string? description,
        int? attendeeCount,
        string? supportChangeReason,
        Guid modifiedByUserId,
        bool isSupportUser)
    {
        var errors = new List<string>();

        // Only owner or support can modify
        if (UserId != modifiedByUserId && !isSupportUser)
        {
            return Result.Forbidden("Only reservation owner or support can modify");
        }

        // Support must provide reason
        if (isSupportUser && string.IsNullOrWhiteSpace(supportChangeReason))
        {
            errors.Add("Support must provide change reason");
        }

        // Cannot modify completed/cancelled reservations
        if (Status is ReservationStatus.COMPLETED or ReservationStatus.CANCELLED or ReservationStatus.NOT_CHECKED_IN)
        {
            errors.Add($"Cannot modify reservation with status {Status}");
        }

        // Validate new times if provided
        var newDate = reservationDate ?? ReservationDate;
        var newStart = startTime ?? StartTime;
        var newEnd = endTime ?? EndTime;

        if (newEnd <= newStart)
        {
            errors.Add("End time must be after start time");
        }

        var duration = newEnd - newStart;
        if (duration < TimeSpan.FromHours(1))
        {
            errors.Add("Reservation must be at least 1 hour");
        }

        if (newEnd > new TimeOnly(23, 59))
        {
            errors.Add("Reservation cannot end after 23:59");
        }

        if (attendeeCount.HasValue && attendeeCount <= 0)
        {
            errors.Add("Attendee count must be positive");
        }

        if (errors.Count > 0)
        {
            return Result.Invalid(errors);
        }

        // Apply changes
        if (reservationDate.HasValue) ReservationDate = reservationDate.Value;
        if (startTime.HasValue) StartTime = startTime.Value;
        if (endTime.HasValue) EndTime = endTime.Value;
        if (title != null) Title = title.Trim();
        if (description != null) Description = description.Trim();
        if (attendeeCount.HasValue) AttendeeCount = attendeeCount.Value;
        if (supportChangeReason != null) SupportChangeReason = supportChangeReason.Trim();

        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseEvent(new ReservationModifiedEvent(
            Id, ResourceId, UserId, ReservationDate, StartTime, EndTime, Title ?? string.Empty,
            modifiedByUserId, isSupportUser, supportChangeReason));

        return Result.Success();
    }

    public Result Cancel(Guid cancelledByUserId, string? reason, bool isSupportUser)
    {
        if (Status is ReservationStatus.CANCELLED or ReservationStatus.COMPLETED or ReservationStatus.NOT_CHECKED_IN)
        {
            return Result.Conflict($"Cannot cancel reservation with status {Status}");
        }

        if (UserId != cancelledByUserId && !isSupportUser)
        {
            return Result.Forbidden("Only reservation owner or support can cancel");
        }

        if (isSupportUser && string.IsNullOrWhiteSpace(reason))
        {
            return Result.Invalid("Support must provide cancellation reason");
        }

        Status = ReservationStatus.CANCELLED;
        CancelledAt = DateTimeOffset.UtcNow;
        CancelledByUserId = cancelledByUserId;
        CancellationReason = reason?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseEvent(new ReservationCancelledEvent(
            Id, ResourceId, UserId, cancelledByUserId, reason ?? string.Empty, isSupportUser));

        return Result.Success();
    }

    public Result CheckIn(Guid checkedInByUserId, string scannedPublicQrId)
    {
        if (UserId != checkedInByUserId)
        {
            return Result.Forbidden("Only reservation owner can check in");
        }

        if (Status != ReservationStatus.CONFIRMED)
        {
            return Result.Conflict($"Cannot check in reservation with status {Status}");
        }

        if (Resource?.RequiresCheckIn != true)
        {
            return Result.Invalid("This resource type does not require check-in");
        }

        if (Resource?.PublicQrId?.ToString() != scannedPublicQrId)
        {
            return Result.Invalid("QR code does not match resource");
        }

        var now = DateTimeOffset.UtcNow;
        var reservationStart = ReservationDate.ToDateTime(StartTime);
        var reservationEnd = ReservationDate.ToDateTime(EndTime);
        var windowStart = reservationStart.AddMinutes(-15);
        var windowEnd = reservationEnd.AddMinutes(15);

        if (now < windowStart || now > windowEnd)
        {
            return Result.Invalid("Check-in only allowed within 15 minutes of reservation window");
        }

        Status = ReservationStatus.CHECKED_IN;
        CheckedInAt = now;
        UpdatedAt = now;

        RaiseEvent(new CheckInCompletedEvent(Id, ResourceId, UserId, scannedPublicQrId, now));

        return Result.Success();
    }

    public Result CheckOut()
    {
        if (Status != ReservationStatus.CHECKED_IN)
        {
            return Result.Conflict("Only checked-in reservations can be checked out");
        }

        Status = ReservationStatus.CHECKED_OUT;
        CheckedOutAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseEvent(new CheckOutCompletedEvent(Id, ResourceId, UserId, DateTimeOffset.UtcNow));

        return Result.Success();
    }

    public void AutoComplete()
    {
        if (Status == ReservationStatus.CONFIRMED)
        {
            Status = ReservationStatus.NOT_CHECKED_IN;
        }
        else if (Status == ReservationStatus.CHECKED_IN)
        {
            Status = ReservationStatus.CHECKED_OUT;
        }
        else if (Status == ReservationStatus.CHECKED_OUT)
        {
            Status = ReservationStatus.COMPLETED;
        }
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public ReservationDto ToDto() => new(
        Id,
        ResourceId,
        Resource?.Code ?? string.Empty,
        Resource?.Name ?? string.Empty,
        Resource?.ResourceTypeCode ?? string.Empty,
        UserId,
        User?.DisplayName ?? string.Empty,
        ReservationDate,
        StartTime,
        EndTime,
        Status,
        Title,
        Description,
        AttendeeCount,
        SupportChangeReason,
        CheckedInAt,
        CheckedOutAt,
        CancelledAt,
        CancellationReason,
        CreatedAt,
        UpdatedAt
    );
}

public record ReservationDto(
    Guid Id,
    Guid ResourceId,
    string ResourceCode,
    string ResourceName,
    string ResourceTypeCode,
    Guid UserId,
    string UserName,
    DateOnly ReservationDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    ReservationStatus Status,
    string? Title,
    string? Description,
    int? AttendeeCount,
    string? SupportChangeReason,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset? CheckedOutAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);