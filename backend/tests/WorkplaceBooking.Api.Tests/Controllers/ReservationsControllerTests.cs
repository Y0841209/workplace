using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MediatR;
using Ardalis.Result;
using WorkplaceBooking.Api.Controllers;
using WorkplaceBooking.Application.UseCases.Commands.Reservations;
using WorkplaceBooking.Application.UseCases.Queries.Reservations;
using WorkplaceBooking.Application.DTOs;

namespace WorkplaceBooking.Api.Tests.Controllers;

public class ReservationsControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly ReservationsController _controller;

    public ReservationsControllerTests()
    {
        _controller = new ReservationsController(_mediator.Object);
    }

    [Fact]
    public async Task Create_Should_Return_Ok_When_Valid()
    {
        var command = new CreateReservationCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(11, 0));
        var expectedDto = new ReservationDto(Guid.NewGuid(), Guid.NewGuid(), "R001", "Test", "OPEN_WORKSPACE", Guid.NewGuid(), "User", "user@test.com", DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(11, 0), "CONFIRMED", null, null, null, null, null, null, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        _mediator.Setup(x => x.Send(It.IsAny<CreateReservationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedDto));

        var result = await _controller.Create(new CreateReservationCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(11, 0)), CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedDto);
    }

    [Fact]
    public async Task Create_Should_Return_BadRequest_When_Invalid()
    {
        _mediator.Setup(x => x.Send(It.IsAny<CreateReservationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Invalid(new[] { new Ardalis.Result.ValidationError("ResourceId", "Resource is required") }));

        var result = await _controller.Create(new CreateReservationCommand(Guid.Empty, DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(11, 0)), CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_Should_Return_Conflict_When_Not_Available()
    {
        _mediator.Setup(x => x.Send(It.IsAny<CreateReservationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Conflict("Resource not available"));

        var result = await _controller.Create(new CreateReservationCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(11, 0)), CancellationToken.None);

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Create_Should_Return_Forbidden_When_Unauthorized()
    {
        _mediator.Setup(x => x.Send(It.IsAny<CreateReservationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Forbidden());

        var result = await _controller.Create(new CreateReservationCommand(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(11, 0)), CancellationToken.None);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetMy_Should_Return_Ok()
    {
        var expected = new PagedResult<ReservationDto>(new List<ReservationDto>(), 0, 1, 20);
        _mediator.Setup(x => x.Send(It.IsAny<GetMyReservationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        var result = await _controller.GetMy(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAvailability_Should_Return_Ok()
    {
        var expected = new List<AvailabilitySlotDto>();
        _mediator.Setup(x => x.Send(It.IsAny<GetAvailabilityQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<AvailabilitySlotDto>>(expected));

        var result = await _controller.GetAvailability(DateOnly.FromDateTime(DateTime.Today.AddDays(1)), new TimeOnly(9, 0), new TimeOnly(11, 0), CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CheckIn_Should_Return_Ok_When_Valid()
    {
        var command = new CheckInReservationCommand(Guid.NewGuid(), Guid.NewGuid());
        var expectedDto = new CheckInDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, null, null);

        _mediator.Setup(x => x.Send(It.IsAny<CheckInReservationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedDto));

        var result = await _controller.CheckIn(Guid.NewGuid(), command, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CheckIn_Should_Return_BadRequest_When_Id_Mismatch()
    {
        var id = Guid.NewGuid();
        var command = new CheckInReservationCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await _controller.CheckIn(id, command, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Cancel_Should_Return_Ok_When_Valid()
    {
        _mediator.Setup(x => x.Send(It.IsAny<CancelReservationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Cancel(Guid.NewGuid(), new CancelReservationCommand(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }
}