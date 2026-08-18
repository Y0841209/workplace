using FluentAssertions;
using WorkplaceBooking.Application.Validators;
using WorkplaceBooking.Application.UseCases.Commands.Reservations;

namespace WorkplaceBooking.Application.Tests.Validators;

public class CreateReservationValidatorTests
{
    private readonly CreateReservationValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid_Command()
    {
        var command = new CreateReservationCommand(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            "Test",
            "Description",
            2);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_ResourceId_Empty()
    {
        var command = new CreateReservationCommand(
            Guid.Empty,
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            new TimeOnly(9, 0),
            new TimeOnly(11, 0));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ResourceId");
    }

    [Fact]
    public void Should_Fail_When_Date_In_Past()
    {
        var command = new CreateReservationCommand(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            new TimeOnly(9, 0),
            new TimeOnly(11, 0));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReservationDate");
    }

    [Fact]
    public void Should_Fail_When_Start_After_End()
    {
        var command = new CreateReservationCommand(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            new TimeOnly(11, 0),
            new TimeOnly(9, 0));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("before"));
    }

    [Fact]
    public void Should_Fail_When_Duration_Less_Than_1_Hour()
    {
        var command = new CreateReservationCommand(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            new TimeOnly(9, 0),
            new TimeOnly(9, 30));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("1 hour"));
    }

    [Fact]
    public void Should_Fail_When_End_After_23_59()
    {
        var command = new CreateReservationCommand(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            new TimeOnly(22, 0),
            new TimeOnly(23, 59).AddMinutes(1));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("23:59"));
    }

    [Fact]
    public void Should_Fail_When_Attendee_Count_Zero()
    {
        var command = new CreateReservationCommand(
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            AttendeeCount: 0);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AttendeeCount");
    }
}