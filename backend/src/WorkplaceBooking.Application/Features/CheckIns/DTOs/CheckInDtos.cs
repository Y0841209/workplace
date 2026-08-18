using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Application.Features.CheckIns.DTOs;

public record CheckInDto(
    Guid Id,
    Guid ReservationId,
    Guid ResourceId,
    Guid UserId,
    DateTimeOffset CheckedInAt,
    string? IpAddress,
    string? UserAgent);

public record CheckInHistoryQueryDto(
    int Page = 1,
    int PageSize = 20,
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