namespace WorkplaceBooking.Domain.Entities;

public class ReservationException : Entity, IAuditableEntity
{
    public Guid UserId { get; private set; }
    public int MaximumFutureActiveReservations { get; private set; }
    public string? AppliesToResourceTypeCode { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly ExpiresAt { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public bool Active { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public AppUser? User { get; private set; }
    public ResourceType? AppliesToResourceType { get; private set; }
    public AppUser? CreatedByUser { get; private set; }

    private ReservationException() { }

    private ReservationException(Guid id, Guid userId, int maximumFutureActiveReservations, string? appliesToResourceTypeCode, DateOnly validFrom, DateOnly expiresAt, string reason, Guid createdByUserId)
        : base(id)
    {
        UserId = userId;
        MaximumFutureActiveReservations = maximumFutureActiveReservations;
        AppliesToResourceTypeCode = appliesToResourceTypeCode;
        ValidFrom = validFrom;
        ExpiresAt = expiresAt;
        Reason = reason;
        Active = true;
        CreatedByUserId = createdByUserId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<ReservationException> Create(
        Guid userId,
        int maximumFutureActiveReservations,
        string? appliesToResourceTypeCode,
        DateOnly validFrom,
        DateOnly expiresAt,
        string reason,
        Guid createdByUserId)
    {
        if (userId == Guid.Empty)
            return Result.Failure(new Error("EXCEPTION_USER_REQUIRED", "User is required"));

        if (maximumFutureActiveReservations <= 0)
            return Result.Failure(new Error("EXCEPTION_LIMIT_INVALID", "Maximum reservations must be positive"));

        if (expiresAt < validFrom)
            return Result.Failure(new Error("EXCEPTION_DATES_INVALID", "Expires date must be after valid from date"));

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(new Error("EXCEPTION_REASON_REQUIRED", "Reason is required"));

        if (createdByUserId == Guid.Empty)
            return Result.Failure(new Error("EXCEPTION_CREATOR_REQUIRED", "Creator is required"));

        return Result.Success(new ReservationException(Guid.NewGuid(), userId, maximumFutureActiveReservations, appliesToResourceTypeCode, validFrom, expiresAt, reason, createdByUserId));
    }

    public void Update(int? maximumFutureActiveReservations = null, DateOnly? validFrom = null, DateOnly? expiresAt = null, string? reason = null, bool? active = null)
    {
        if (maximumFutureActiveReservations.HasValue)
        {
            if (maximumFutureActiveReservations <= 0)
                throw new DomainException("Maximum reservations must be positive", "EXCEPTION_LIMIT_INVALID");
            MaximumFutureActiveReservations = maximumFutureActiveReservations.Value;
        }

        if (validFrom.HasValue) ValidFrom = validFrom.Value;
        if (expiresAt.HasValue)
        {
            if (expiresAt.Value < ValidFrom)
                throw new DomainException("Expires date must be after valid from date", "EXCEPTION_DATES_INVALID");
            ExpiresAt = expiresAt.Value;
        }

        if (reason != null) Reason = reason;
        if (active.HasValue) Active = active.Value;

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActiveOn(DateOnly date) =>
        Active && date >= ValidFrom && date <= ExpiresAt;
}