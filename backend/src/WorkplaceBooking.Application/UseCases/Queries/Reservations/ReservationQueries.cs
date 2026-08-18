using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;

namespace WorkplaceBooking.Application.UseCases.Queries.Reservations;

public record GetMyReservationsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null) : IRequest<Result<PagedResult<ReservationDto>>>;

public record GetReservationByIdQuery(
    Guid ReservationId) : IRequest<Result<ReservationDto>>;

public record GetAvailabilityQuery(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? ResourceTypeCode = null,
    Guid? FloorId = null,
    Guid? ZoneId = null,
    int? MinCapacity = null) : IRequest<Result<IReadOnlyList<AvailabilitySlotDto>>>;

public record GetResourceByQrQuery(
    Guid PublicQrId) : IRequest<Result<AvailabilitySlotDto>>;