using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Application.Interfaces;

namespace WorkplaceBooking.Application.UseCases.Commands.Reservations;

public class CancelReservationHandler : IRequestHandler<CancelReservationCommand, Result>
{
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CancelReservationHandler(
        IRepository<Reservation> reservationRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _reservationRepository = reservationRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");
        var isSupportUser = _currentUser.IsInRole("SUPPORT");

        var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken);
        if (reservation == null)
            return Result.NotFound("Reservation not found");

        // Check ownership
        if (reservation.UserId != _currentUser.UserId && !isSupportUser)
            return Result.Forbidden("Only reservation owner or support can cancel");

        // Support must provide reason
        if (isSupportUser && string.IsNullOrWhiteSpace(request.Reason))
            return Result.Invalid(new[] {
                new Error("SUPPORT_REASON_REQUIRED", "Support must provide cancellation reason")
            });

        if (reservation.Status is ReservationStatus.CANCELLED or ReservationStatus.COMPLETED or ReservationStatus.NOT_CHECKED_IN)
            return Result.Error($"Cannot cancel reservation with status {reservation.Status}");

        var result = reservation.Cancel(_currentUser.UserId!.Value, request.Reason, isSupportUser);
        if (!result.IsSuccess)
            return Result.Error(result.Errors.First().Message);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}