using Ardalis.Result;
using Ardalis.Specification;
using BookingPlatform.Domain.Entities;
using BookingPlatform.Domain.Enums;
using BookingPlatform.Domain.Events;
using BookingPlatform.Domain.Interfaces;
using BookingPlatform.Domain.Specifications;
using BookingPlatform.Domain.ValueObjects;
using MediatR;

namespace BookingPlatform.Application.UseCases.Reservations.Commands;

public record CreateReservationCommand(
    Guid ResourceId,
    DateOnly ReservationDate,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Title,
    string? Description,
    int? AttendeeCount
) : IRequest<Result<ReservationDto>>;

public class CreateReservationHandler : IRequestHandler<CreateReservationCommand, Result<ReservationDto>>
{
    private readonly IRepository<Resource> _resourceRepository;
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly IRepository<AppUser> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IPolicyService _policyService;
    private readonly IAvailabilityService _availabilityService;
    private readonly ISettingsService _settingsService;
    private readonly IExceptionService _exceptionService;
    private readonly ILogger<CreateReservationHandler> _logger;

    public CreateReservationHandler(
        IRepository<Resource> resourceRepository,
        IRepository<Reservation> reservationRepository,
        IRepository<AppUser> userRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IPolicyService policyService,
        IAvailabilityService availabilityService,
        ISettingsService settingsService,
        IExceptionService exceptionService,
        ILogger<CreateReservationHandler> logger)
    {
        _resourceRepository = resourceRepository;
        _reservationRepository = reservationRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _policyService = policyService;
        _availabilityService = availabilityService;
        _settingsService = settingsService;
        _exceptionService = exceptionService;
        _logger = logger;
    }

    public async Task<Result<ReservationDto>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

        // 1. Validate resource exists and is reservable
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource == null || !resource.Active || !resource.Reservable)
        {
            return Result.NotFound("Resource not found or not reservable");
        }

        // 2. Check user can reserve this resource type
        var canReserve = await _policyService.CanReserveAsync(userId, resource.ResourceTypeCode, cancellationToken);
        if (!canReserve)
        {
            return Result.Forbidden("User not authorized to reserve this resource type");
        }

        // 3. Check availability
        var isAvailable = await _availabilityService.IsAvailableAsync(
            request.ResourceId,
            request.ReservationDate,
            request.StartTime,
            request.EndTime,
            cancellationToken);

        if (!isAvailable)
        {
            return Result.Conflict("Resource not available for selected time slot");
        }

        // 4. Validate business rules
        var validationResult = ValidateBusinessRules(request);
        if (!validationResult.IsSuccess)
        {
            return validationResult;
        }

        // 5. Check future reservation limit
        var futureCount = await _reservationRepository.CountAsync(
            new FutureActiveReservationsSpec(userId),
            cancellationToken);

        var maxReservations = await _settingsService.GetMaxFutureReservationsAsync(cancellationToken);
        var hasException = await _exceptionService.HasActiveExceptionAsync(userId, resource.ResourceTypeCode, cancellationToken);

        if (futureCount >= maxReservations && !hasException)
        {
            return Result.Error($"Maximum {maxReservations} future active reservations exceeded");
        }

        // 6. Create reservation
        var createdByUserId = userId;
        var reservation = Reservation.Create(
            resourceId: request.ResourceId,
            userId: userId,
            createdByUserId: createdByUserId,
            reservationDate: request.ReservationDate,
            startTime: request.StartTime,
            endTime: request.EndTime,
            title: request.Title,
            description: request.Description,
            attendeeCount: request.AttendeeCount);

        if (!reservation.IsSuccess)
        {
            return Result.Error(reservation.Errors.FirstOrDefault()?.Message ?? "Failed to create reservation");
        }

        await _reservationRepository.AddAsync(reservation.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Raise domain event (will be captured by EF Core interceptor for audit/notification)
        reservation.Value.RaiseEvent(new ReservationCreatedEvent(
            reservation.Value.Id,
            reservation.Value.ResourceId,
            reservation.Value.UserId,
            reservation.Value.ReservationDate,
            reservation.Value.StartTime,
            reservation.Value.EndTime,
            reservation.Value.Title ?? string.Empty));

        _logger.LogInformation("Reservation {ReservationId} created by user {UserId} for resource {ResourceId}",
            reservation.Value.Id, userId, request.ResourceId);

        return Result.Success(reservation.Value.ToDto());
    }

    private Result ValidateBusinessRules(CreateReservationCommand request)
    {
        var errors = new List<string>();

        // Minimum duration: 1 hour
        var duration = TimeSpan.FromTicks((request.ReservationDate.ToDateTime(request.EndTime) - request.ReservationDate.ToDateTime(request.StartTime)).Ticks);
        if (duration < TimeSpan.FromHours(1))
        {
            errors.Add("Reservation must be at least 1 hour");
        }

        // Same day only
        if (request.StartTime >= request.EndTime)
        {
            errors.Add("End time must be after start time");
        }

        // Max end time: 23:59
        if (request.EndTime > new TimeOnly(23, 59))
        {
            errors.Add("Reservation cannot end after 23:59");
        }

        // Attendee count validation for meeting rooms
        if (request.AttendeeCount.HasValue && request.AttendeeCount <= 0)
        {
            errors.Add("Attendee count must be positive");
        }

        return errors.Count > 0 ? Result.Invalid(errors) : Result.Success();
    }
}