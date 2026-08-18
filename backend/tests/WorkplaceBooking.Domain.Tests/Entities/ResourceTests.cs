using FluentAssertions;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.SharedKernel.Results;

namespace WorkplaceBooking.Domain.Tests.Entities;

public class ResourceTests
{
    [Fact]
    public void Create_Should_Succeed_For_OpenWorkspace_With_Qr()
    {
        var result = Resource.Create(
            "P03-OA-001",
            "Oficina abierta P03 001",
            "OPEN_WORKSPACE",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            1,
            Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Value.ResourceTypeCode.Should().Be("OPEN_WORKSPACE");
        result.Value.PublicQrId.Should().NotBeNull();
        result.Value.QrVersion.Should().Be(1);
    }

    [Fact]
    public void Create_Should_Succeed_For_ClosedOffice_With_Qr()
    {
        var result = Resource.Create(
            "P03-OC-001",
            "Oficina cerrada P03 001",
            "CLOSED_OFFICE",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            1,
            Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Value.ResourceTypeCode.Should().Be("CLOSED_OFFICE");
        result.Value.PublicQrId.Should().NotBeNull();
    }

    [Fact]
    public void Create_Should_Succeed_For_MeetingRoom_Without_Qr()
    {
        var result = Resource.Create(
            "SJ-01",
            "Sala de juntas 01",
            "MEETING_ROOM",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            12,
            null);

        result.IsSuccess.Should().BeTrue();
        result.Value.ResourceTypeCode.Should().Be("MEETING_ROOM");
        result.Value.PublicQrId.Should().BeNull();
    }

    [Fact]
    public void Create_Should_Fail_When_OpenWorkspace_Without_Qr()
    {
        var result = Resource.Create(
            "P03-OA-001",
            "Oficina abierta",
            "OPEN_WORKSPACE",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            1,
            null);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "RESOURCE_QR_REQUIRED");
    }

    [Fact]
    public void Create_Should_Fail_When_MeetingRoom_With_Qr()
    {
        var result = Resource.Create(
            "SJ-01",
            "Sala de juntas",
            "MEETING_ROOM",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            12,
            Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "RESOURCE_QR_FORBIDDEN");
    }

    [Fact]
    public void Create_Should_Fail_When_Capacity_Zero()
    {
        var result = Resource.Create(
            "P03-OA-001",
            "Oficina abierta",
            "OPEN_WORKSPACE",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            0,
            Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "RESOURCE_CAPACITY_INVALID");
    }

    [Fact]
    public void Update_Should_Regenerate_Qr_For_Office()
    {
        var createResult = Resource.Create(
            "P03-OA-001",
            "Oficina abierta",
            "OPEN_WORKSPACE",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            1,
            Guid.NewGuid());

        var resource = createResult.Value;
        var oldQrId = resource.PublicQrId;
        var oldVersion = resource.QrVersion;

        resource.RegenerateQr();

        resource.PublicQrId.Should().NotBe(oldQrId);
        resource.QrVersion.Should().Be(oldVersion + 1);
    }

    [Fact]
    public void Update_Should_Fail_When_Regenerate_Qr_For_MeetingRoom()
    {
        var createResult = Resource.Create(
            "SJ-01",
            "Sala de juntas",
            "MEETING_ROOM",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            12,
            null);

        var resource = createResult.Value;

        var act = () => resource.RegenerateQr();

        act.Should().Throw<DomainException>()
            .WithMessage("*MEETING_ROOM*");
    }

    [Fact]
    public void Update_Should_Fail_When_Changing_To_MeetingRoom_With_Qr()
    {
        var createResult = Resource.Create(
            "P03-OA-001",
            "Oficina abierta",
            "OPEN_WORKSPACE",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            1,
            Guid.NewGuid());

        var resource = createResult.Value;

        var act = () => resource.Update(
            resourceTypeCode: "MEETING_ROOM",
            publicQrId: resource.PublicQrId);

        act.Should().Throw<DomainException>()
            .WithMessage("*not allowed*");
    }

    [Fact]
    public void Update_Should_Fail_When_Removing_Qr_From_Office()
    {
        var createResult = Resource.Create(
            "P03-OA-001",
            "Oficina abierta",
            "OPEN_WORKSPACE",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            1,
            Guid.NewGuid());

        var resource = createResult.Value;

        var act = () => resource.Update(
            publicQrId: null);

        act.Should().Throw<DomainException>()
            .WithMessage("*required*");
    }
}