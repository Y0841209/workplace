using WorkplaceBooking.SharedKernel.Primitives;
using WorkplaceBooking.SharedKernel.Results;

namespace WorkplaceBooking.Domain.Entities;

public enum NotificationType
{
    RESERVATION_CREATED,
    RESERVATION_MODIFIED,
    RESERVATION_CANCELLED,
    RESERVATION_REMINDER
}

public enum NotificationStatus
{
    PENDING,
    SENT,
    FAILED,
    CANCELLED
}

public class NotificationOutbox : Entity, IAuditableEntity
{
    public Guid? ReservationId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string RecipientEmail { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public NotificationStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public Reservation? Reservation { get; private set; }
    public AppUser? RecipientUser { get; private set; }

    private NotificationOutbox() { }

    private NotificationOutbox(
        Guid id,
        Guid? reservationId,
        Guid recipientUserId,
        string recipientEmail,
        NotificationType type,
        string subject,
        string body,
        DateTimeOffset scheduledAt)
        : base(id)
    {
        ReservationId = reservationId;
        RecipientUserId = recipientUserId;
        RecipientEmail = recipientEmail;
        Type = type;
        Subject = subject;
        Body = body;
        ScheduledAt = scheduledAt;
        SentAt = null;
        Status = NotificationStatus.PENDING;
        RetryCount = 0;
        LastError = null;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<NotificationOutbox> Create(
        Guid? reservationId,
        Guid recipientUserId,
        string recipientEmail,
        NotificationType type,
        string subject,
        string body,
        DateTimeOffset scheduledAt)
    {
        if (recipientUserId == Guid.Empty)
            return Result.Failure<NotificationOutbox>(new Error("NOTIFICATION_RECIPIENT_REQUIRED", "Recipient user is required"));

        if (string.IsNullOrWhiteSpace(recipientEmail))
            return Result.Failure<NotificationOutbox>(new Error("NOTIFICATION_EMAIL_REQUIRED", "Recipient email is required"));

        if (string.IsNullOrWhiteSpace(subject))
            return Result.Failure<NotificationOutbox>(new Error("NOTIFICATION_SUBJECT_REQUIRED", "Subject is required"));

        if (string.IsNullOrWhiteSpace(body))
            return Result.Failure<NotificationOutbox>(new Error("NOTIFICATION_BODY_REQUIRED", "Body is required"));

        return Result.Success(new NotificationOutbox(Guid.NewGuid(), reservationId, recipientUserId, recipientEmail, type, subject, body, scheduledAt));
    }

    public void MarkSent()
    {
        Status = NotificationStatus.SENT;
        SentAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string error)
    {
        Status = NotificationStatus.FAILED;
        LastError = error;
        RetryCount++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCancelled()
    {
        Status = NotificationStatus.CANCELLED;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}