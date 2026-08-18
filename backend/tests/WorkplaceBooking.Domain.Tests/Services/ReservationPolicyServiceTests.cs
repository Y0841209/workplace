using FluentAssertions;
using Moq;
using WorkplaceBooking.Domain.Entities;
using WorkplaceBooking.Domain.Interfaces;
using WorkplaceBooking.Domain.Services;
using WorkplaceBooking.Domain.Specifications;

namespace WorkplaceBooking.Domain.Tests.Services;

public class ReservationPolicyServiceTests
{
    private readonly Mock<IRepository<AppSettings>> _settingsRepo = new();
    private readonly Mock<IRepository<ReservationException>> _exceptionRepo = new();
    private readonly Mock<IRepository<UserApplicationRole>> _userRoleRepo = new();
    private readonly Mock<IRepository<UserBusinessProfile>> _userProfileRepo = new();
    private readonly Mock<IRepository<ResourceAccessPolicy>> _policyRepo = new();

    private readonly ReservationPolicyService _service;

    public ReservationPolicyServiceTests()
    {
        _service = new ReservationPolicyService(
            _settingsRepo.Object,
            _exceptionRepo.Object,
            _userRoleRepo.Object,
            _userProfileRepo.Object,
            _policyRepo.Object);
    }

    [Fact]
    public async Task GetMaxFutureReservationsAsync_Should_Return_Default_When_No_Settings()
    {
        _settingsRepo.Setup(x => x.FirstOrDefaultAsync(It.IsAny<SingleAppSettingsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppSettings?)null);

        var result = await _service.GetMaxFutureReservationsAsync();

        result.Should().Be(5);
    }

    [Fact]
    public async Task HasActiveExceptionAsync_Should_Return_True_When_Exception_Exists()
    {
        var userId = Guid.NewGuid();
        var exception = ReservationException.Create(userId, 10, "MEETING_ROOM", DateOnly.FromDateTime(DateTime.Today), DateOnly.FromDateTime(DateTime.Today.AddDays(30)), "Test", Guid.NewGuid()).Value;

        _exceptionRepo.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveExceptionForUserSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(exception);

        var result = await _service.HasActiveExceptionAsync(Guid.NewGuid(), "MEETING_ROOM");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanReserveAsync_Should_Return_True_For_Global_Admin()
    {
        var userId = Guid.NewGuid();

        _userRoleRepo.Setup(x => x.AnyAsync(It.IsAny<ActiveRoleForUserSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.CanReserveAsync(userId, "OPEN_WORKSPACE");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanReserveAsync_Should_Check_Policies_For_Non_Admin()
    {
        var userId = Guid.NewGuid();

        _userRoleRepo.Setup(x => x.AnyAsync(It.IsAny<ActiveRoleForUserSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var profile = UserBusinessProfile.Create(userId, "LEADER", DateOnly.FromDateTime(DateTime.Today), null, null, "Test").Value;
        _userProfileRepo.Setup(x => x.ListAsync(It.IsAny<ActiveProfilesForUserSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserBusinessProfile> { profile });

        var policy = ResourceAccessPolicy.Create("LEADER", "OPEN_WORKSPACE", true, true, true).Value;
        _policyRepo.Setup(x => x.FirstOrDefaultAsync(It.IsAny<PolicyForProfileAndTypeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(policy);

        var result = await _service.CanReserveAsync(userId, "OPEN_WORKSPACE");

        result.Should().BeTrue();
    }
}