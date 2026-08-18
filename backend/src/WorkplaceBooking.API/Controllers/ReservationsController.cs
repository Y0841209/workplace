using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkplaceBooking.Application.Features.Reservations.Commands;
using WorkplaceBooking.Application.Features.Reservations.Queries;
using WorkplaceBooking.Application.Features.Reservations.DTOs;

namespace WorkplaceBooking.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/reservations")]
[ApiVersion("1.0")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReservationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new reservation
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationDto>> Create([FromBody] CreateReservationCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get all reservations for the current user
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedResult<ReservationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReservationDto>>> GetMy(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var query = new GetMyReservationsQuery(page, pageSize, status, dateFrom, dateTo);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get a specific reservation by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationDto>> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetReservationQuery(id);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Update a reservation
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ReservationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationDto>> Update(Guid id, [FromBody] UpdateReservationCommand command, CancellationToken ct)
    {
        if (id != command.ReservationId)
            return BadRequest("Reservation ID mismatch");

        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Cancel a reservation
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Cancel(Guid id, [FromBody] CancelReservationCommand? command = null, CancellationToken ct = default)
    {
        command ??= new CancelReservationCommand(id);
        if (id != command.ReservationId)
            return BadRequest("Reservation ID mismatch");

        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Check-in to a reservation using QR code
    /// </summary>
    [HttpPost("{id:guid}/check-in")]
    [ProducesResponseType(typeof(CheckInDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CheckInDto>> CheckIn(Guid id, [FromBody] CheckInReservationCommand command, CancellationToken ct)
    {
        if (id != command.ReservationId)
            return BadRequest("Reservation ID mismatch");

        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Check-out from a reservation
    /// </summary>
    [HttpPost("{id:guid}/check-out")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CheckOut(Guid id, CancellationToken ct)
    {
        var command = new CheckOutReservationCommand(id);
        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get available resources for a time slot
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
    /// Get resource info by QR code for check-in (public endpoint)
    /// </summary>
    [HttpGet("check-in/{publicQrId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AvailabilitySlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AvailabilitySlotDto>> GetResourceForCheckIn(Guid publicQrId, CancellationToken ct)
    {
        var query = new GetResourceByQrQuery(publicQrId);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }
}