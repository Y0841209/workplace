using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.DTOs;
using WorkplaceBooking.Application.Interfaces;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.SharedKernel.Results;

namespace WorkplaceBooking.Application.UseCases.Commands.Reservations;

public class CreateReservationHandler : IRequestHandler<CreateReservationCommand, Result<ReservationDto>>
{
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly IRepository<AppUser> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IReservationPolicyService _policyService;
    private readonly IAvailabilityService _availabilityService;

    public CreateReservationHandler(
        IRepository<Resource> resourceRepository,
        IRepository<Reservation> reservationRepository,
        IRepository<AppUser> userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IReservationPolicyService policyService,
        IAvailabilityService availabilityService)
    {
        _resourceRepository = resourceRepository;
        _reservationRepository = reservationRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _policyService = policyService;
        _availabilityService = availabilityService;
    }

    public async Task<Result<ReservationDto>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        // Validate resource exists and is reservable
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource == null || !resource.Active || !resource.Reservable)
            return Result.NotFound("Resource not found or not reservable");

        // Check user can reserve this resource type
        var canReserve = await _policyService.CanReserveAsync(userId, resource.ResourceTypeCode, cancellationToken);
        if (!canReserve)
            return Result.Forbidden("User not authorized to reserve this resource type");

        // Check availability
        var isAvailable = await _availabilityService.IsAvailableAsync(
            request.ResourceId,
            request.ReservationDate,
            request.StartTime,
            request.EndTime,
            cancellationToken);

        if (!isAvailable)
            return Result.Conflict("Resource not available for selected time slot");

        // Check future reservation limit
        var futureCount = await _reservationRepository.CountAsync(
            new FutureActiveReservationsSpec(_currentUser.UserId!.Value),
            cancellationToken);

        var maxReservations = await _policyService.GetMaxFutureReservationsAsync(cancellationToken);
        var hasException = await _policyService.HasActiveExceptionAsync(
            userId, resource.ResourceTypeCode, cancellationToken);

        if (futureCount >= maxReservations && !hasException)
            return Result.Error($"Maximum {maxReservations} future active reservations exceeded");

        // Validate attendee count for meeting rooms
        if (request.AttendeeCount.HasValue && resource.ResourceTypeCode == "MEETING_ROOM")
        {
            if (request.AttendeeCount > resource.Capacity)
                return Result.Invalid(new[] {
                    new Error("ATTENDEE_COUNT_EXCEEDS_CAPACITY", $"Attendee count ({request.AttendeeCount}) exceeds room capacity ({resource.Capacity})")
                });
        }

        // Create reservation
        var createdByUserId = _currentUser.UserId!.Value;
        var reservationResult = Reservation.Create(
            request.ResourceId,
            userId,
            createdByUserId,
            request.ReservationDate,
            request.StartTime,
            request.EndTime,
            request.Title,
            request.Description,
            request.AttendeeCount);

        if (!reservationResult.IsSuccess)
            return Result.Error(reservationResult.Errors.First().Message);

        var reservation = reservationResult.Value;
        await _reservationRepository.AddAsync(reservation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ReservationDto(
            reservation.Id,
            reservation.ResourceId,
            resource.Code,
            resource.Name,
            resource.ResourceTypeCode,
            reservation.UserId,
            _currentUser.DisplayName ?? string.Empty,
            _currentUser.Email ?? string.Empty,
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