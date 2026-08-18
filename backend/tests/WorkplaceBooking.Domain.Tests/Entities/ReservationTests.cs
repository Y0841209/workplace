using FluentAssertions;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.SharedKernel.Results;

namespace WorkplaceBooking.Domain.Tests.Entities;

public class ReservationTests
{
    [Fact]
    public void Create_Should_Succeed_When_Valid_Data()
    {
        var result = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            new TimeOnly(9, 0),
            new TimeOnly(11, 0),
            "Test",
            "Description",
            2);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ReservationStatus.CONFIRMED);
        result.Value.Title.Should().Be("Test");
        result.Value.AttendeeCount.Should().Be(2);
    }

    [Fact]
    public void Create_Should_Fail_When_ResourceId_Empty()
    {
        var result = Reservation.Create(
            Guid.Empty,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            new TimeOnly(9, 0),
            new TimeOnly(11, 0));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "RESERVATION_RESOURCE_REQUIRED");
    }

    [Fact]
    public void Create_Should_Fail_When_End_Before_Start()
    {
        var result = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            new TimeOnly(11, 0),
            new TimeOnly(9, 0));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "RESERVATION_TIME_ORDER_INVALID");
    }

    [Fact]
    public void Create_Should_Fail_When_Duration_Less_Than_1_Hour()
    {
        var result = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            new TimeOnly(9, 0),
            new TimeOnly(9, 30));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "RESERVATION_MIN_DURATION");
    }

    [Fact]
    public void Create_Should_Fail_When_End_After_23_59()
    {
        var result = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            new TimeOnly(22, 0),
            new TimeOnly(23, 59).AddMinutes(1));

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "RESERVATION_MAX_END_TIME");
    }

    [Fact]
    public void Modify_Should_Fail_When_Not_Owner_And_Not_Support()
    {
        var createResult = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            new TimeOnly(9, 0),
            new TimeOnly(11, 0));

        var reservation = createResult.Value;

        var result = reservation.Modify(
            startTime: new TimeOnly(10, 0),
            endTime: new TimeOnly(12, 0),
            modifiedByUserId: Guid.NewGuid(), // Different user
            isSupportUser: false);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "RESERVATION_MODIFY_FORBIDDEN");
    }

    [Fact]
    public void Cancel_Should_Fail_When_Not_Owner_And_Not_Support()
    {
        var createResult = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            new TimeOnly(9, 0),
            new TimeOnly(11, 0));

        var reservation = createResult.Value;

        var result = reservation.Cancel(Guid.NewGuid(), "Reason", false);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "RESERVATION_CANCEL_FORBIDDEN");
    }

    [Fact]
    public void Cancel_Should_Fail_When_Already_Cancelled()
    {
        var createResult = Reservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            new TimeOnly(9, 0),
            new TimeOnly(11, 0));

        var reservation = createResult.Value;
        reservation.Cancel(reservation.UserId, "Reason", false);

        var result = reservation.Cancel(reservation.UserId, "Another reason", false);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "RESERVATION_CANNOT_CANCEL");
    }
}