using FluentAssertions;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.SharedKernel.Results;

namespace WorkplaceBooking.Domain.Tests.Entities;

public class CheckInTests
{
    [Fact]
    public void Create_Should_Succeed_When_Valid_Data()
    {
        var result = CheckIn.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "192.168.1.1",
            "Mozilla/5.0");

        result.IsSuccess.Should().BeTrue();
        result.Value.Method.Should().Be(CheckInMethod.QR);
        result.Value.CheckedInAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        result.Value.IpAddress.Should().Be("192.168.1.1");
        result.Value.UserAgent.Should().Be("Mozilla/5.0");
    }

    [Fact]
    public void Create_Should_Fail_When_ReservationId_Empty()
    {
        var result = CheckIn.Create(
            Guid.Empty,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "CHECKIN_RESERVATION_REQUIRED");
    }

    [Fact]
    public void Create_Should_Fail_When_ResourceId_Empty()
    {
        var result = CheckIn.Create(
            Guid.NewGuid(),
            Guid.Empty,
            Guid.NewGuid(),
            Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "CHECKIN_RESOURCE_REQUIRED");
    }

    [Fact]
    public void Create_Should_Fail_When_UserId_Empty()
    {
        var result = CheckIn.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty,
            Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "CHECKIN_USER_REQUIRED");
    }

    [Fact]
    public void Create_Should_Fail_When_QrId_Empty()
    {
        var result = CheckIn.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "CHECKIN_QR_REQUIRED");
    }
}