using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Common.DTOs;
using WorkplaceBooking.Application.Features.Reservations.DTOs;

namespace WorkplaceBooking.Application.Features.Reservations.Queries;

public record GetReservationQuery(
    Guid ReservationId) : IRequest<Ardalis.Result.Result<ReservationDto>>;

public record GetMyReservationsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null) : IRequest<Ardalis.Result.Result<WorkplaceBooking.Application.Common.DTOs.PagedResult<ReservationDto>>>;