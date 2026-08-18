using FluentAssertions;
using Moq;
using MediatR;
using Ardalis.Result;
using WorkplaceBooking.Api.Controllers;
using WorkplaceBooking.Application.UseCases.Commands.Reservations;
using WorkplaceBooking.Application.UseCases.Queries.Reservations;
using WorkplaceBooking.Application.UseCases.Queries.CheckIns;
using WorkplaceBooking.Application.DTOs;

namespace WorkplaceBooking.Api.Tests.Controllers;

public class ReservationsControllerCheckInTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly ReservationsController _controller;

    public ReservationsControllerCheckInTests()
    {
        _controller = new ReservationsController(_mediator.Object);
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
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedDto);
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
    public async Task CheckOut_Should_Return_Ok_When_Valid()
    {
        _mediator.Setup(x => x.Send(It.IsAny<CheckOutReservationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.CheckOut(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task GetResourceForCheckIn_Should_Return_Ok()
    {
        var expectedDto = new AvailabilitySlotDto(Guid.NewGuid(), "P03-OA-001", "Oficina abierta", "OPEN_WORKSPACE", Guid.NewGuid(), "Piso 3", null, null, 1, TimeOnly.MinValue, TimeOnly.MaxValue, true);
        _mediator.Setup(x => x.Send(It.IsAny<GetResourceByQrQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedDto));

        var result = await _controller.GetResourceForCheckIn(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }
}