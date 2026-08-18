using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Features.Reservations.DTOs;

namespace WorkplaceBooking.Application.Features.Reservations.Queries;

public record GetReservationQuery(
    Guid ReservationId) : IRequest<Result<ReservationDto>>;

public record GetMyReservationsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null) : IRequest<Result<PagedResult<ReservationDto>>>;