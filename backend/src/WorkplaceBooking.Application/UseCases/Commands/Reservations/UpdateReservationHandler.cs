using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;
using WorkplaceBooking.Application.Interfaces;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Entities.Reservation;

namespace WorkplaceBooking.Application.UseCases.Commands.Reservations;

public class UpdateReservationHandler : IRequestHandler<UpdateReservationCommand, Result<ReservationDto>>
{
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IAvailabilityService _availabilityService;

    public UpdateReservationHandler(
        IRepository<Reservation> reservationRepository,
        IRepository<Resource> resourceRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IAvailabilityService availabilityService)
    {
        _reservationRepository = reservationRepository;
        _resourceRepository = resourceRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _availabilityService = availabilityService;
    }

    public async Task<Result<ReservationDto>> Handle(UpdateReservationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");
        var isSupportUser = _currentUser.IsInRole("SUPPORT");

        var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken);
        if (reservation == null)
            return Result.NotFound("Reservation not found");

        // Check ownership
        if (reservation.UserId != userId && !isSupportUser)
            return Result.Forbidden("Only reservation owner or support can modify");

        // Support must provide reason
        if (isSupportUser && string.IsNullOrWhiteSpace(request.SupportChangeReason))
            return Result.Invalid(new[] {
                new Error("SUPPORT_REASON_REQUIRED", "Support must provide change reason")
            });

        // Cannot modify completed/cancelled
        if (reservation.Status is ReservationStatus.COMPLETED or ReservationStatus.CANCELLED or ReservationStatus.NOT_CHECKED_IN)
            return Result.Error($"Cannot modify reservation with status {reservation.Status}");

        // Validate time changes
        var newDate = request.ReservationDate ?? reservation.ReservationDate;
        var newStart = request.StartTime ?? reservation.StartTime;
        var newEnd = request.EndTime ?? reservation.EndTime;

        if (newEnd <= newStart)
            return Result.Invalid(new[] { new Error("TIME_ORDER_INVALID", "End time must be after start time") });

        if (newEnd - newStart < TimeSpan.FromHours(1))
            return Result.Invalid(new[] { new Error("MIN_DURATION", "Reservation must be at least 1 hour") });

        if (newEnd > new TimeOnly(23, 59))
            return Result.Invalid(new[] { new Error("MAX_END_TIME", "Reservation cannot end after 23:59") });

        // Check availability for new time slot (excluding current reservation)
        var isAvailable = await _availabilityService.IsAvailableAsync(
            reservation.ResourceId,
            newDate,
            newStart,
            newEnd,
            cancellationToken,
            excludeReservationId: reservation.Id);

        if (!isAvailable)
            return Result.Conflict("Resource not available for new time slot");

        // Apply changes
        var modifyResult = reservation.Modify(
            request.ReservationDate,
            request.StartTime,
            request.EndTime,
            request.Title,
            request.Description,
            request.AttendeeCount,
            request.SupportChangeReason,
            _currentUser.UserId,
            isSupportUser);

        if (!modifyResult.IsSuccess)
            return Result.Error(modifyResult.Errors.First().Message);

        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        // Return updated DTO (would need to fetch updated entity)
        return Result.Success(ToDto(reservation));
    }

    private ReservationDto ToDto(Reservation r) => new ReservationDto(
        r.Id, r.ResourceId, string.Empty, string.Empty, string.Empty,
        r.UserId, string.Empty, string.Empty,
        r.ReservationDate, r.StartTime, r.EndTime,
        r.Status.ToString(), r.Title, r.Description, r.AttendeeCount,
        r.SupportChangeReason, r.CheckedInAt, r.CheckedOutAt,
        r.CancelledAt, r.CancellationReason, r.CreatedAt, r.UpdatedAt);
}