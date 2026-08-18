using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkplaceBooking.Application.Features.CheckIns.Queries;
using WorkplaceBooking.Application.Features.CheckIns.DTOs;

namespace WorkplaceBooking.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/check-ins")]
[ApiVersion("1.0")]
[Authorize]
public class CheckInsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CheckInsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get check-in history for the current user
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResult<CheckInDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CheckInDto>>> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null,
        CancellationToken ct = default)
    {
        var query = new GetCheckInHistoryQuery(page, pageSize, dateFrom, dateTo);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get check-ins for a specific resource
    /// </summary>
    [HttpGet("resource/{resourceId:guid}")]
    [Authorize(Roles = "GLOBAL_ADMIN,SUPPORT")]
    [ProducesResponseType(typeof(IReadOnlyList<CheckInDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CheckInDto>>> GetByResource(
        Guid resourceId,
        [FromQuery] DateOnly? date = null,
        CancellationToken ct = default)
    {
        var query = new GetResourceCheckInsQuery(resourceId, date);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get today's check-ins for the current user
    /// </summary>
    [HttpGet("today")]
    [ProducesResponseType(typeof(IReadOnlyList<CheckInDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CheckInDto>>> GetTodays(CancellationToken ct)
    {
        var query = new GetTodaysCheckInsQuery();
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }
}