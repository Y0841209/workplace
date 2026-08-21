using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Common.Interfaces;
using WorkplaceBooking.Application.Features.Reservations.Commands;
using WorkplaceBooking.Application.Features.Reservations.DTOs;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.Features.Reservations.Handlers;

public class UpdateReservationHandler : IRequestHandler<UpdateReservationCommand, Ardalis.Result.Result<ReservationDto>>
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
            return Ardalis.Result.Result.Invalid(new[] { new ValidationError("SUPPORT_REASON_REQUIRED", "Support must provide change reason", "SUPPORT_REASON_REQUIRED", ValidationSeverity.Error) });

        // Cannot modify completed/cancelled
        if (reservation.Status is ReservationStatus.COMPLETED or ReservationStatus.CANCELLED or ReservationStatus.NOT_CHECKED_IN)
            return Ardalis.Result.Result.Error($"Cannot modify reservation with status {reservation.Status}");

        // Validate time changes
        var newDate = request.ReservationDate ?? reservation.ReservationDate;
        var newStart = request.StartTime ?? reservation.StartTime;
        var newEnd = request.EndTime ?? reservation.EndTime;

        if (newEnd <= newStart)
            return Ardalis.Result.Result.Invalid(new[] { new ValidationError("TIME_ORDER_INVALID", "End time must be after start time", "TIME_ORDER_INVALID", ValidationSeverity.Error) });

        if (newEnd - newStart < TimeSpan.FromHours(1))
            return Ardalis.Result.Result.Invalid(new[] { new ValidationError("MIN_DURATION", "Reservation must be at least 1 hour", "MIN_DURATION", ValidationSeverity.Error) });

        if (newEnd > new TimeOnly(23, 59))
            return Ardalis.Result.Result.Invalid(new[] { new ValidationError("MAX_END_TIME", "Reservation cannot end after 23:59", "MAX_END_TIME", ValidationSeverity.Error) });

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
            return Ardalis.Result.Result.Error(modifyResult.Error.Message);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ReservationDto(
            reservation.Id,
            reservation.ResourceId,
            string.Empty, // Would need to load resource
            string.Empty,
            string.Empty,
            reservation.UserId,
            string.Empty,
            string.Empty,
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