using FluentValidation;
using WorkplaceBooking.Application.Features.Reservations.Commands;
using WorkplaceBooking.Application.Common.Interfaces;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Application.Features.Reservations.Validators;

public class UpdateReservationValidator : AbstractValidator<UpdateReservationCommand>
{
    public UpdateReservationValidator()
    {
        RuleFor(x => x.ReservationId)
            .NotEmpty().WithMessage("Reservation ID is required");

        When(x => x.StartTime.HasValue && x.EndTime.HasValue, () =>
        {
            RuleFor(x => x.EndTime!.Value)
                .GreaterThan(x => x.StartTime!.Value)
                .WithMessage("End time must be after start time");

            RuleFor(x => x.EndTime!.Value)
                .LessThanOrEqualTo(new TimeOnly(23, 59))
                .WithMessage("End time cannot exceed 23:59");

            RuleFor(x => x)
                .Must(x => (x.EndTime!.Value - x.StartTime!.Value) >= TimeSpan.FromHours(1))
                .WithMessage("Reservation must be at least 1 hour");
        });

        RuleFor(x => x.AttendeeCount)
            .GreaterThan(0).When(x => x.AttendeeCount.HasValue)
            .WithMessage("Attendee count must be positive");

        When(x => x.SupportChangeReason != null, () =>
        {
            RuleFor(x => x.SupportChangeReason)
                .NotEmpty().WithMessage("Support change reason is required");
        });
    }
}

public class CancelReservationValidator : AbstractValidator<CancelReservationCommand>
{
    public CancelReservationValidator()
    {
        RuleFor(x => x.ReservationId)
            .NotEmpty().WithMessage("Reservation ID is required");
    }
}