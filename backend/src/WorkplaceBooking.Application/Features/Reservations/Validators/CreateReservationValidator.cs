using FluentValidation;
using WorkplaceBooking.Application.Common.Interfaces;
using WorkplaceBooking.Application.Features.Reservations.Commands;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.Features.Reservations.Validators;

public class CreateReservationValidator : AbstractValidator<CreateReservationCommand>
{
    private readonly IRepository<Reservation> _reservationRepository;
    private readonly IReservationPolicyService _policyService;
    private readonly ICurrentUserService _currentUserService;

    public CreateReservationValidator(
        IRepository<Reservation> reservationRepository,
        IReservationPolicyService policyService,
        ICurrentUserService currentUserService)
    {
        _reservationRepository = reservationRepository;
        _policyService = policyService;
        _currentUserService = currentUserService;

        RuleFor(x => x.ResourceId)
            .NotEmpty().WithMessage("Resource is required");

        RuleFor(x => x.ReservationDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("Reservation date cannot be in the past");

        RuleFor(x => x.StartTime)
            .LessThan(x => x.EndTime)
            .WithMessage("Start time must be before end time");

        RuleFor(x => x.EndTime)
            .LessThanOrEqualTo(new TimeOnly(23, 59))
            .WithMessage("End time cannot exceed 23:59");

        RuleFor(x => x)
            .Must(x => (x.EndTime - x.StartTime) >= TimeSpan.FromHours(1))
            .WithMessage("Reservation must be at least 1 hour");

        RuleFor(x => x)
            .Must(x => x.ReservationDate.ToDateTime(x.StartTime).Date == x.ReservationDate.ToDateTime(x.EndTime).Date)
            .WithMessage("Reservation must start and end on the same day");

        RuleFor(x => x.AttendeeCount)
            .GreaterThan(0).When(x => x.AttendeeCount.HasValue)
            .WithMessage("Attendee count must be positive");

        RuleFor(x => x)
            .MustAsync(async (command, cancellation) => await ValidateFutureReservationLimitAsync(command, cancellation))
            .WithMessage("Maximum 5 future active reservations exceeded");

        RuleFor(x => x)
            .MustAsync(async (command, cancellation) => await ValidateNoOverlappingReservationAsync(command, cancellation))
            .WithMessage("Resource already reserved for the selected time slot");
    }

    private async Task<bool> ValidateFutureReservationLimitAsync(CreateReservationCommand command, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue) return false;

        var maxReservations = await _policyService.GetMaxFutureReservationsAsync(cancellationToken);
        var hasException = await _policyService.HasActiveExceptionAsync(userId.Value, command.ResourceId.ToString(), cancellationToken);

        if (hasException) return true;

        var futureCount = await _reservationRepository.CountAsync(
            new FutureActiveReservationsSpec(userId.Value),
            cancellationToken);

        return futureCount < maxReservations;
    }

    private async Task<bool> ValidateNoOverlappingReservationAsync(CreateReservationCommand command, CancellationToken cancellationToken)
    {
        var spec = new OverlappingReservationSpec(
            command.ResourceId,
            command.ReservationDate,
            command.StartTime,
            command.EndTime);

        var overlapping = await _reservationRepository.AnyAsync(spec, cancellationToken);
        return !overlapping;
    }
}