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