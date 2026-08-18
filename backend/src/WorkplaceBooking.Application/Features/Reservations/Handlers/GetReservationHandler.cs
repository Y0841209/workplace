using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Common.Interfaces;
using WorkplaceBooking.Application.Features.Reservations.DTOs;
using WorkplaceBooking.Application.Features.Reservations.Queries;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.Features.Reservations.Handlers;

public class GetReservationHandler : IRequestHandler<GetReservationQuery, Result<ReservationDto>>
{
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<AppUser> _userRepository;

    public GetReservationHandler(
        IRepository<Reservation> reservationRepository,
        IRepository<Resource> resourceRepository,
        IRepository<AppUser> userRepository)
    {
        _reservationRepository = reservationRepository;
        _resourceRepository = resourceRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<ReservationDto>> Handle(GetReservationQuery request, CancellationToken cancellationToken)
    {
        var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken);
        if (reservation == null)
            return Result.NotFound("Reservation not found");

        var resource = await _resourceRepository.GetByIdAsync(reservation.ResourceId, CancellationToken.None);
        var user = await _userRepository.GetByIdAsync(reservation.UserId, CancellationToken.None);

        return Result.Success(new ReservationDto(
            reservation.Id,
            reservation.ResourceId,
            resource?.Code ?? string.Empty,
            resource?.Name ?? string.Empty,
            resource?.ResourceTypeCode ?? string.Empty,
            reservation.UserId,
            user?.DisplayName ?? string.Empty,
            user?.Email ?? string.Empty,
            reservation.ReservationDate,
            reservation.StartTime,
            reservation.EndTime,
            reservation.Status.ToString(),
            reservation.Title,
            reservation.Description,
            reservation.AttendeeCount,
            reservation.SupportChangeReason,
            reservation.CheckedInAt,
            reservation.CheckedOutAt,
            reservation.CancelledAt,
            reservation.CancellationReason,
            reservation.CreatedAt,
            reservation.UpdatedAt));
    }
}