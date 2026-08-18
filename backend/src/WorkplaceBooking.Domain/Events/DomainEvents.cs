namespace WorkplaceBooking.Domain.Events;

public record ReservationCreatedEvent(Reservation Reservation) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
    public Guid ReservationId => Reservation.Id;
    public Guid ResourceId => Reservation.ResourceId;
    public Guid UserId => Reservation.UserId;
    public DateOnly ReservationDate => Reservation.ReservationDate;
    public TimeOnly StartTime => Reservation.StartTime;
    public TimeOnly EndTime => Reservation.EndTime;
}

public record ReservationModifiedEvent(Reservation Reservation) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
    public Guid ReservationId => Reservation.Id;
    public Guid ResourceId => Reservation.ResourceId;
    public Guid UserId => Reservation.UserId;
    public DateOnly ReservationDate => Reservation.ReservationDate;
    public TimeOnly StartTime => Reservation.StartTime;
    public TimeOnly EndTime => Reservation.EndTime;
}

public record ReservationCancelledEvent(Reservation Reservation) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
    public Guid ReservationId => Reservation.Id;
    public Guid ResourceId => Reservation.ResourceId;
    public Guid UserId => Reservation.UserId;
    public string? CancellationReason => Reservation.CancellationReason;
    public Guid? CancelledByUserId => Reservation.CancelledByUserId;
}

public record CheckInCompletedEvent(Reservation Reservation) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
    public Guid ReservationId => Reservation.Id;
    public Guid ResourceId => Reservation.ResourceId;
    public Guid UserId => Reservation.UserId;
    public DateTimeOffset CheckedInAt => Reservation.CheckedInAt!.Value;
}

public record CheckOutCompletedEvent(Reservation Reservation) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
    public Guid ReservationId => Reservation.Id;
    public Guid ResourceId => Reservation.ResourceId;
    public Guid UserId => Reservation.UserId;
    public DateTimeOffset CheckedOutAt => Reservation.CheckedOutAt!.Value;
}

public record UserProfileAssignedEvent(UserBusinessProfile Profile) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
    public Guid UserId => Profile.UserId;
    public string ProfileCode => Profile.ProfileCode;
    public DateOnly ValidFrom => Profile.ValidFrom;
}

public record UserRoleAssignedEvent(UserApplicationRole Role) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
    public Guid UserId => Role.UserId;
    public string RoleCode => Role.RoleCode;
    public DateOnly ValidFrom => Role.ValidFrom;
}

public record ExceptionCreatedEvent(ReservationException Exception) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
    public Guid UserId => Exception.UserId;
    public int MaximumFutureActiveReservations => Exception.MaximumFutureActiveReservations;
    public string? AppliesToResourceTypeCode => Exception.AppliesToResourceTypeCode;
}

public record ResourceCreatedEvent(Resource Resource) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
    public Guid ResourceId => Resource.Id;
    public string ResourceCode => Resource.Code;
    public string ResourceTypeCode => Resource.ResourceTypeCode;
}

public record ResourceModifiedEvent(Resource Resource) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
    public Guid ResourceId => Resource.Id;
    public string ResourceCode => Resource.Code;
}

public record ResourceDeletedEvent(Guid ResourceId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
    public Guid ResourceId { get; }
}