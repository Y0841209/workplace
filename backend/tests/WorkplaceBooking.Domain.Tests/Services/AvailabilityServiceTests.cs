using FluentAssertions;
using Moq;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Services;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Domain.Tests.Services;

public class AvailabilityServiceTests
{
    private readonly Mock<IRepository<Reservation>> _reservationRepo = new();
    private readonly Mock<IRepository<Resource>> _resourceRepo = new();

    private readonly AvailabilityService _service;

    public AvailabilityServiceTests()
    {
        _service = new AvailabilityService(_reservationRepo.Object, _resourceRepo.Object);
    }

    [Fact]
    public async Task IsAvailableAsync_Should_Return_False_When_Resource_Not_Found()
    {
        _resourceRepo.Setup(x => x.GetByIdAsync(It.IsAny<ResourceByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Resource?)null);

        var result = await _service.IsAvailableAsync(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(11, 0));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_Should_Return_False_When_Resource_Inactive()
    {
        var resource = Resource.Create("R001", "Test", "OPEN_WORKSPACE", Guid.NewGuid(), Guid.NewGuid(), null, 1, Guid.NewGuid()).Value;
        resource.GetType().GetProperty("Active")!.SetValue(resource, false);

        _resourceRepo.Setup(x => x.GetByIdAsync(It.IsAny<ResourceByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);

        var result = await _service.IsAvailableAsync(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(11, 0));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_Should_Return_False_When_Overlapping_Reservation_Exists()
    {
        var resourceId = Guid.NewGuid();
        var resource = Resource.Create("R001", "Test", "OPEN_WORKSPACE", Guid.NewGuid(), Guid.NewGuid(), null, 1, Guid.NewGuid()).Value;

        _resourceRepo.Setup(x => x.GetByIdAsync(It.IsAny<ResourceByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);

        var existingReservation = Reservation.Create(resourceId, Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(11, 0)).Value;

        _reservationRepo.Setup(x => x.AnyAsync(It.IsAny<OverlappingReservationSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.IsAvailableAsync(resourceId, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(11, 0));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_Should_Return_True_When_No_Conflicts()
    {
        var resourceId = Guid.NewGuid();
        var resource = Resource.Create("R001", "Test", "OPEN_WORKSPACE", Guid.NewGuid(), Guid.NewGuid(), null, 1, Guid.NewGuid()).Value;

        _resourceRepo.Setup(x => x.GetByIdAsync(It.IsAny<ResourceByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);

        _reservationRepo.Setup(x => x.AnyAsync(It.IsAny<OverlappingReservationSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.IsAvailableAsync(resourceId, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(11, 0));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_Should_Exclude_Specific_Reservation()
    {
        var resourceId = Guid.NewGuid();
        var excludeId = Guid.NewGuid();
        var resource = Resource.Create("R001", "Test", "OPEN_WORKSPACE", Guid.NewGuid(), Guid.NewGuid(), null, 1, Guid.NewGuid()).Value;

        _resourceRepo.Setup(x => x.GetByIdAsync(It.IsAny<ResourceByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resource);

        _reservationRepo.Setup(x => x.AnyAsync(It.Is<OverlappingReservationSpec>(s => s.ExcludeReservationId == excludeId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.IsAvailableAsync(resourceId, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(11, 0), excludeReservationId: excludeId);

        result.Should().BeTrue();
    }
}