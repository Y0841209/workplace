using WorkplaceBooking.SharedKernel.Primitives;
using WorkplaceBooking.SharedKernel.Results;

namespace WorkplaceBooking.Domain.Entities;

public enum CheckInMethod
{
    QR
}

public class CheckIn : Entity, IAuditableEntity
{
    public Guid ReservationId { get; private set; }
    public Guid ResourceId { get; private set; }
    public Guid UserId { get; private set; }
    public CheckInMethod Method { get; private set; }
    public Guid ScannedPublicQrId { get; private set; }
    public DateTimeOffset CheckedInAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public Reservation? Reservation { get; private set; }
    public Resource? Resource { get; private set; }
    public AppUser? User { get; private set; }

    private CheckIn() { }

    private CheckIn(
        Guid id,
        Guid reservationId,
        Guid resourceId,
        Guid userId,
        Guid scannedPublicQrId,
        string? ipAddress = null,
        string? userAgent = null)
        : base(id)
    {
        ReservationId = reservationId;
        ResourceId = resourceId;
        UserId = userId;
        Method = CheckInMethod.QR;
        ScannedPublicQrId = scannedPublicQrId;
        CheckedInAt = DateTimeOffset.UtcNow;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<CheckIn> Create(
        Guid reservationId,
        Guid resourceId,
        Guid userId,
        Guid scannedPublicQrId,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (reservationId == Guid.Empty)
            return Result.Failure<CheckIn>(new Error("CHECKIN_RESERVATION_REQUIRED", "Reservation is required"));

        if (resourceId == Guid.Empty)
            return Result.Failure<CheckIn>(new Error("CHECKIN_RESOURCE_REQUIRED", "Resource is required"));

        if (userId == Guid.Empty)
            return Result.Failure<CheckIn>(new Error("CHECKIN_USER_REQUIRED", "User is required"));

        if (scannedPublicQrId == Guid.Empty)
            return Result.Failure<CheckIn>(new Error("CHECKIN_QR_REQUIRED", "Scanned QR is required"));

        return Result.Success(new CheckIn(Guid.NewGuid(), reservationId, resourceId, userId, scannedPublicQrId, null, null));
    }
}