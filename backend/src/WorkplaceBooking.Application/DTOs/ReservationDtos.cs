namespace WorkplaceBooking.Application.DTOs;

public record ReservationDto(
    Guid Id,
    Guid ResourceId,
    string ResourceCode,
    string ResourceName,
    string ResourceTypeCode,
    Guid UserId,
    string UserName,
    string UserEmail,
    DateOnly ReservationDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string Status,
    string? Title,
    string? Description,
    int? AttendeeCount,
    string? SupportChangeReason,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset? CheckedOutAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CheckInDto(
    Guid Id,
    Guid ReservationId,
    Guid ResourceId,
    Guid UserId,
    DateTimeOffset CheckedInAt,
    string? IpAddress,
    string? UserAgent);

public record AvailabilitySlotDto(
    Guid ResourceId,
    string ResourceCode,
    string ResourceName,
    string ResourceTypeCode,
    Guid FloorId,
    string FloorName,
    Guid? ZoneId,
    string? ZoneName,
    int Capacity,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool Available);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}