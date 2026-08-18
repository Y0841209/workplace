using FluentAssertions;
using WorkplaceBooking.Application.Validators;
using WorkplaceBooking.Application.UseCases.Commands.Resources;

namespace WorkplaceBooking.Application.Tests.Validators;

public class CreateResourceValidatorTests
{
    private readonly CreateResourceValidator _validator = new();

    [Fact]
    public void Should_Pass_When_Valid_OpenWorkspace()
    {
        var command = new CreateResourceCommand(
            "P03-OA-001",
            "Oficina abierta P03 001",
            "OPEN_WORKSPACE",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            1,
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_Valid_ClosedOffice()
    {
        var command = new CreateResourceCommand(
            "P03-OC-001",
            "Oficina cerrada P03 001",
            "CLOSED_OFFICE",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            1,
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Pass_When_Valid_MeetingRoom()
    {
        var command = new CreateResourceCommand(
            "SJ-01",
            "Sala de juntas 01",
            "MEETING_ROOM",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            12,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_Invalid_ResourceType()
    {
        var command = new CreateResourceCommand(
            "TEST-001",
            "Test",
            "INVALID_TYPE",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            1,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ResourceTypeCode");
    }

    [Fact]
    public void Should_Fail_When_OpenWorkspace_Without_Qr()
    {
        var command = new CreateResourceCommand(
            "P03-OA-001",
            "Oficina abierta",
            "OPEN_WORKSPACE",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            1,
            null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("QR code is required"));
    }

    [Fact]
    public void Should_Fail_When_MeetingRoom_With_Qr()
    {
        var command = new CreateResourceCommand(
            "SJ-01",
            "Sala de juntas",
            "MEETING_ROOM",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            12,
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("not allowed"));
    }

    [Fact]
    public void Should_Fail_When_Capacity_Zero()
    {
        var command = new CreateResourceCommand(
            "P03-OA-001",
            "Oficina abierta",
            "OPEN_WORKSPACE",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            0,
            Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Capacity");
    }
}