using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Application.Interfaces;

namespace WorkplaceBooking.Application.UseCases.Commands.Reservations;

public class CheckOutReservationHandler : IRequestHandler<CheckOutReservationCommand, Result>
{
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CheckOutReservationHandler(
        IRepository<Reservation> reservationRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _reservationRepository = reservationRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CheckOutReservationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken);
        if (reservation == null)
            return Result.NotFound("Reservation not found");

        if (reservation.UserId != _currentUser.UserId)
            return Result.Forbidden("Only reservation owner can check out");

        if (reservation.Status != ReservationStatus.CHECKED_IN)
            return Result.Error($"Cannot check out reservation with status {reservation.Status}");

        var result = reservation.CheckOut();
        if (!result.IsSuccess)
            return Result.Error(result.Errors.First().Message);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}