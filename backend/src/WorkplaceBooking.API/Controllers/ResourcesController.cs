using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkplaceBooking.Api.Extensions;
using WorkplaceBooking.Application.Common.DTOs;
using WorkplaceBooking.Application.Features.Resources.Commands;
using WorkplaceBooking.Application.Features.Resources.Queries;
using WorkplaceBooking.Application.Features.Resources.DTOs;
using ArdalisResult = Ardalis.Result;

namespace WorkplaceBooking.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/resources")]
[ApiVersion("1.0")]
[Authorize]
public class ResourcesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ResourcesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new resource (GLOBAL_ADMIN only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "GLOBAL_ADMIN")]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResourceDto>> Create([FromBody] CreateResourceCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get all resources with filtering and pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ResourceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ResourceDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? resourceTypeCode = null,
        [FromQuery] Guid? floorId = null,
        [FromQuery] Guid? zoneId = null,
        [FromQuery] bool? active = null,
        [FromQuery] bool? reservable = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = new GetResourcesQuery(page, pageSize, resourceTypeCode, floorId, zoneId, active, reservable, search);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult<PagedResult<ResourceDto>>();
    }

    /// <summary>
    /// Get all resource types
    /// </summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(IReadOnlyList<ResourceTypeDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ResourceTypeDto>>> GetTypes(CancellationToken ct)
    {
        var query = new GetResourceTypesQuery();
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get resource by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResourceDto>> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetResourceByIdQuery(id);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Update a resource (GLOBAL_ADMIN only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "GLOBAL_ADMIN")]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResourceDto>> Update(Guid id, [FromBody] UpdateResourceCommand command, CancellationToken ct)
    {
        if (id != command.ResourceId)
            return BadRequest("Resource ID mismatch");

        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Delete a resource (GLOBAL_ADMIN only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "GLOBAL_ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var command = new DeleteResourceCommand(id);
        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Regenerate QR code for a resource (GLOBAL_ADMIN only)
    /// </summary>
    [HttpPost("{id:guid}/regenerate-qr")]
    [Authorize(Roles = "GLOBAL_ADMIN")]
    [ProducesResponseType(typeof(ResourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResourceDto>> RegenerateQr(Guid id, CancellationToken ct)
    {
        var command = new RegenerateResourceQrCommand(id);
        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Bulk import resources (GLOBAL_ADMIN only)
    /// </summary>
    [HttpPost("import")]
    [Authorize(Roles = "GLOBAL_ADMIN")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<int>> Import([FromBody] ImportResourcesCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get available resources for a given time slot
    /// </summary>
    [HttpGet("availability")]
    [ProducesResponseType(typeof(IReadOnlyList<AvailabilitySlotDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AvailabilitySlotDto>>> GetAvailability(
        [FromQuery] DateOnly date,
        [FromQuery] TimeOnly startTime,
        [FromQuery] TimeOnly endTime,
        [FromQuery] string? resourceTypeCode = null,
        [FromQuery] Guid? floorId = null,
        [FromQuery] Guid? zoneId = null,
        [FromQuery] int? minCapacity = null,
        CancellationToken ct = default)
    {
        var query = new GetAvailabilityQuery(date, startTime, endTime, resourceTypeCode, floorId, zoneId, minCapacity);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get resources grouped by floor
    /// </summary>
    [HttpGet("by-floor/{floorId:guid}")]
    [ProducesResponseType(typeof(ResourcesByFloorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResourcesByFloorDto>> GetByFloor(
        Guid floorId,
        [FromQuery] string? resourceTypeCode = null,
        [FromQuery] bool? active = null,
        [FromQuery] bool? reservable = null,
        CancellationToken ct = default)
    {
        var query = new GetResourcesByFloorQuery(floorId, resourceTypeCode, active, null);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get meeting rooms with optional filters
    /// </summary>
    [HttpGet("meeting-rooms")]
    [ProducesResponseType(typeof(IReadOnlyList<ResourceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ResourceDto>>> GetMeetingRooms(
        [FromQuery] Guid? floorId = null,
        [FromQuery] int? minCapacity = null,
        [FromQuery] bool? active = null,
        CancellationToken ct = default)
    {
        var query = new GetMeetingRoomsQuery(floorId, minCapacity, active);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }
}