using FluentValidation;
using WorkplaceBooking.Application.UseCases.Commands.Resources;

namespace WorkplaceBooking.Application.Validators;

public class CreateResourceValidator : AbstractValidator<CreateResourceCommand>
{
    public CreateResourceValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .MaximumLength(50).WithMessage("Code must not exceed 50 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.ResourceTypeCode)
            .NotEmpty().WithMessage("Resource type is required")
            .Must(code => new[] { "OPEN_WORKSPACE", "CLOSED_OFFICE", "MEETING_ROOM" }.Contains(code))
            .WithMessage("Resource type must be OPEN_WORKSPACE, CLOSED_OFFICE, or MEETING_ROOM");

        RuleFor(x => x.LocationId)
            .NotEmpty().WithMessage("Location is required");

        RuleFor(x => x.FloorId)
            .NotEmpty().WithMessage("Floor is required");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Capacity must be positive");

        When(x => x.PublicQrId.HasValue, () =>
        {
            RuleFor(x => x.PublicQrId)
                .NotEqual(Guid.Empty).WithMessage("Public QR ID cannot be empty");
        });
    }
}

public class UpdateResourceValidator : AbstractValidator<UpdateResourceCommand>
{
    public UpdateResourceValidator()
    {
        RuleFor(x => x.ResourceId)
            .NotEmpty().WithMessage("Resource ID is required");

        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name!)
                .NotEmpty().WithMessage("Name cannot be empty")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
        });

        When(x => x.ResourceTypeCode != null, () =>
        {
            RuleFor(x => x.ResourceTypeCode!)
                .Must(code => new[] { "OPEN_WORKSPACE", "CLOSED_OFFICE", "MEETING_ROOM" }.Contains(code))
                .WithMessage("Resource type must be OPEN_WORKSPACE, CLOSED_OFFICE, or MEETING_ROOM");
        });

        When(x => x.Capacity.HasValue, () =>
        {
            RuleFor(x => x.Capacity!.Value)
                .GreaterThan(0).WithMessage("Capacity must be positive");
        });

        When(x => x.PublicQrId.HasValue, () =>
        {
            RuleFor(x => x.PublicQrId!.Value)
                .NotEqual(Guid.Empty).WithMessage("Public QR ID cannot be empty");
        });
    }
}

public class DeleteResourceValidator : AbstractValidator<DeleteResourceCommand>
{
    public DeleteResourceValidator()
    {
        RuleFor(x => x.ResourceId)
            .NotEmpty().WithMessage("Resource ID is required");
    }
}

public class RegenerateResourceQrValidator : AbstractValidator<RegenerateResourceQrCommand>
{
    public RegenerateResourceQrValidator()
    {
        RuleFor(x => x.ResourceId)
            .NotEmpty().WithMessage("Resource ID is required");
    }
}

public class ImportResourcesValidator : AbstractValidator<ImportResourcesCommand>
{
    public ImportResourcesValidator()
    {
        RuleFor(x => x.Resources)
            .NotEmpty().WithMessage("At least one resource is required");

        RuleForEach(x => x.Resources)
            .SetValidator(new CreateResourceValidator());
    }
}