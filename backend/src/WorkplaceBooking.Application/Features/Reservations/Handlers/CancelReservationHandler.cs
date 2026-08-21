using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Common.Interfaces;
using WorkplaceBooking.Application.Features.Reservations.Commands;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;

namespace WorkplaceBooking.Application.Features.Reservations.Handlers;

public class CancelReservationHandler : IRequestHandler<CancelReservationCommand, Ardalis.Result.Result>
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

    public async Task<Ardalis.Result.Result> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");
        var isSupportUser = _currentUser.IsInRole("SUPPORT");

        var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken);
        if (reservation == null)
            return Ardalis.Result.Result.NotFound("Reservation not found");

        // Check ownership
        if (reservation.UserId != userId && !isSupportUser)
            return Ardalis.Result.Result.Forbidden("Only reservation owner or support can cancel");

        // Support must provide reason
        if (isSupportUser && string.IsNullOrWhiteSpace(request.Reason))
            return Ardalis.Result.Result.Invalid(new[] { new ValidationError("SUPPORT_REASON_REQUIRED", "Support must provide cancellation reason", "SUPPORT_REASON_REQUIRED", ValidationSeverity.Error) });

        if (reservation.Status is ReservationStatus.CANCELLED or ReservationStatus.COMPLETED or ReservationStatus.NOT_CHECKED_IN)
            return Ardalis.Result.Result.Error($"Cannot cancel reservation with status {reservation.Status}");

        var result = reservation.Cancel(_currentUser.UserId!.Value, request.Reason, isSupportUser);
        if (!result.IsSuccess)
            return Ardalis.Result.Result.Error(result.Error.Message);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ardalis.Result.Result.Success();
    }
}