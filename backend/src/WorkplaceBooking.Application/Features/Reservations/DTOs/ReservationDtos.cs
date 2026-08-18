using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Application.Features.Reservations.DTOs;

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

public record CreateReservationDto(
    Guid ResourceId,
    DateOnly ReservationDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Title = null,
    string? Description = null,
    int? AttendeeCount = null);

public record UpdateReservationDto(
    DateOnly? ReservationDate = null,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    string? Title = null,
    string? Description = null,
    int? AttendeeCount = null,
    string? SupportChangeReason = null);

public record ReservationListQueryDto(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}