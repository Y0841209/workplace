using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MediatR;
using Ardalis.Result;
using WorkplaceBooking.Api.Controllers;
using WorkplaceBooking.Application.UseCases.Commands.Resources;
using WorkplaceBooking.Application.UseCases.Queries.Resources;
using WorkplaceBooking.Application.DTOs;

namespace WorkplaceBooking.Api.Tests.Controllers;

public class ResourcesControllerTests
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly ResourcesController _controller;

    public ResourcesControllerTests()
    {
        _controller = new ResourcesController(_mediator.Object);
    }

    [Fact]
    public async Task Create_Should_Return_Ok_When_Valid()
    {
        var command = new CreateResourceCommand("P03-OA-001", "Oficina abierta", "OPEN_WORKSPACE", Guid.NewGuid(), Guid.NewGuid(), null, 1, Guid.NewGuid());
        var expectedDto = new ResourceDto(Guid.NewGuid(), "P03-OA-001", "Oficina abierta", "OPEN_WORKSPACE", "Oficina abierta", Guid.NewGuid(), "Sede principal", Guid.NewGuid(), "P03", null, null, 1, Guid.NewGuid(), 1, true, true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        _mediator.Setup(x => x.Send(It.IsAny<CreateResourceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedDto));

        var result = await _controller.Create(command, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedDto);
    }

    [Fact]
    public async Task Create_Should_Return_BadRequest_When_Invalid()
    {
        _mediator.Setup(x => x.Send(It.IsAny<CreateResourceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Invalid(new[] { new Ardalis.Result.ValidationError("ResourceTypeCode", "Resource type must be OPEN_WORKSPACE, CLOSED_OFFICE, or MEETING_ROOM") }));

        var result = await _controller.Create(new CreateResourceCommand("TEST-001", "Test", "INVALID", Guid.NewGuid(), Guid.NewGuid(), null, 1, Guid.NewGuid()), CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_Should_Return_Conflict_When_Code_Exists()
    {
        _mediator.Setup(x => x.Send(It.IsAny<CreateResourceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Conflict("Resource with code 'P03-OA-001' already exists"));

        var result = await _controller.Create(new CreateResourceCommand("P03-OA-001", "Test", "OPEN_WORKSPACE", Guid.NewGuid(), Guid.NewGuid(), null, 1, Guid.NewGuid()), CancellationToken.None);

        result.Result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task GetAll_Should_Return_Ok()
    {
        var expected = new PagedResult<ResourceDto>(new List<ResourceDto>(), 0, 1, 20);
        _mediator.Setup(x => x.Send(It.IsAny<GetResourcesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expected));

        var result = await _controller.GetAll(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetTypes_Should_Return_Ok()
    {
        var expected = new List<ResourceTypeDto>
        {
            new("OPEN_WORKSPACE", "Oficina abierta", true, true, true),
            new("CLOSED_OFFICE", "Oficina cerrada", true, true, true),
            new("MEETING_ROOM", "Sala de juntas", false, false, true)
        };

        _mediator.Setup(x => x.Send(It.IsAny<GetResourceTypesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<ResourceTypeDto>>(expected));

        var result = await _controller.GetTypes(CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_Should_Return_Ok()
    {
        var expectedDto = new ResourceDto(Guid.NewGuid(), "P03-OA-001", "Oficina abierta", "OPEN_WORKSPACE", "Oficina abierta", Guid.NewGuid(), "Sede principal", Guid.NewGuid(), "P03", null, null, 1, Guid.NewGuid(), 1, true, true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        _mediator.Setup(x => x.Send(It.IsAny<GetResourceByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedDto));

        var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Should_Return_Ok_When_Valid()
    {
        var command = new UpdateResourceCommand(Guid.NewGuid(), Name: "Nuevo nombre");
        var expectedDto = new ResourceDto(Guid.NewGuid(), "P03-OA-001", "Nuevo nombre", "OPEN_WORKSPACE", "Oficina abierta", Guid.NewGuid(), "Sede principal", Guid.NewGuid(), "P03", null, null, 1, Guid.NewGuid(), 1, true, true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        _mediator.Setup(x => x.Send(It.IsAny<UpdateResourceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedDto));

        var result = await _controller.Update(Guid.NewGuid(), command, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_Should_Return_BadRequest_When_Id_Mismatch()
    {
        var id = Guid.NewGuid();
        var command = new UpdateResourceCommand(Guid.NewGuid(), Name: "Test");

        var result = await _controller.Update(id, command, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Delete_Should_Return_Ok_When_Valid()
    {
        _mediator.Setup(x => x.Send(It.IsAny<DeleteResourceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await _controller.Delete(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task RegenerateQr_Should_Return_Ok_When_Valid()
    {
        var expectedDto = new ResourceDto(Guid.NewGuid(), "P03-OA-001", "Oficina abierta", "OPEN_WORKSPACE", "Oficina abierta", Guid.NewGuid(), "Sede principal", Guid.NewGuid(), "P03", null, null, 1, Guid.NewGuid(), 2, true, true, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        _mediator.Setup(x => x.Send(It.IsAny<RegenerateResourceQrCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(expectedDto));

        var result = await _controller.RegenerateQr(Guid.NewGuid(), CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Import_Should_Return_Ok()
    {
        _mediator.Setup(x => x.Send(It.IsAny<ImportResourcesCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(5));

        var command = new ImportResourcesCommand(new List<CreateResourceCommand>());
        var result = await _controller.Import(command, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }
}