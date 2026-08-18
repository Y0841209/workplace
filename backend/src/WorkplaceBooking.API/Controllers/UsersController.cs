using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorkplaceBooking.Application.Features.Users.Commands;
using WorkplaceBooking.Application.Features.Users.Queries;
using WorkplaceBooking.Application.Features.Users.DTOs;
using WorkplaceBooking.Application.Common.Interfaces;

namespace WorkplaceBooking.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/users")]
[ApiVersion("1.0")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public UsersController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken ct)
    {
        var query = new GetCurrentUserQuery();
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "GLOBAL_ADMIN,SUPPORT")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken ct)
    {
        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Assign a business profile to a user (GLOBAL_ADMIN only)
    /// </summary>
    [HttpPost("{userId:guid}/profiles")]
    [Authorize(Roles = "GLOBAL_ADMIN")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserProfileDto>> AssignProfile(Guid userId, [FromBody] AssignProfileCommand command, CancellationToken ct)
    {
        if (userId != command.UserId)
            return BadRequest("User ID mismatch");

        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Assign an administrative role to a user (GLOBAL_ADMIN only)
    /// </summary>
    [HttpPost("{userId:guid}/roles")]
    [Authorize(Roles = "GLOBAL_ADMIN")]
    [ProducesResponseType(typeof(UserRoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserRoleDto>> AssignRole(Guid userId, [FromBody] AssignRoleCommand command, CancellationToken ct)
    {
        if (userId != command.UserId)
            return BadRequest("User ID mismatch");

        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Create a reservation exception for a user (GLOBAL_ADMIN only)
    /// </summary>
    [HttpPost("{userId:guid}/exceptions")]
    [Authorize(Roles = "GLOBAL_ADMIN")]
    [ProducesResponseType(typeof(ExceptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ExceptionDto>> CreateException(Guid userId, [FromBody] CreateExceptionCommand command, CancellationToken ct)
    {
        if (userId != command.UserId)
            return BadRequest("User ID mismatch");

        var result = await _mediator.Send(command, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get current user's profiles
    /// </summary>
    [HttpGet("me/profiles")]
    [ProducesResponseType(typeof(IReadOnlyList<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserProfileDto>>> GetMyProfiles(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");
        var query = new GetUserProfilesQuery(userId);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get current user's roles
    /// </summary>
    [HttpGet("me/roles")]
    [ProducesResponseType(typeof(IReadOnlyList<UserRoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserRoleDto>>> GetMyRoles(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");
        var query = new GetUserRolesQuery(userId);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get current user's exceptions
    /// </summary>
    [HttpGet("me/exceptions")]
    [ProducesResponseType(typeof(IReadOnlyList<ExceptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ExceptionDto>>> GetMyExceptions(CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("User not authenticated");
        var query = new GetUserExceptionsQuery(userId);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get user profiles (GLOBAL_ADMIN/SUPPORT only)
    /// </summary>
    [HttpGet("{userId:guid}/profiles")]
    [Authorize(Roles = "GLOBAL_ADMIN,SUPPORT")]
    [ProducesResponseType(typeof(IReadOnlyList<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<UserProfileDto>>> GetUserProfiles(Guid userId, CancellationToken ct)
    {
        var query = new GetUserProfilesQuery(userId);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get user roles (GLOBAL_ADMIN/SUPPORT only)
    /// </summary>
    [HttpGet("{userId:guid}/roles")]
    [Authorize(Roles = "GLOBAL_ADMIN,SUPPORT")]
    [ProducesResponseType(typeof(IReadOnlyList<UserRoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<UserRoleDto>>> GetUserRoles(Guid userId, CancellationToken ct)
    {
        var query = new GetUserRolesQuery(userId);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }

    /// <summary>
    /// Get user exceptions (GLOBAL_ADMIN/SUPPORT only)
    /// </summary>
    [HttpGet("{userId:guid}/exceptions")]
    [Authorize(Roles = "GLOBAL_ADMIN,SUPPORT")]
    [ProducesResponseType(typeof(IReadOnlyList<ExceptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ExceptionDto>>> GetUserExceptions(Guid userId, CancellationToken ct)
    {
        var query = new GetUserExceptionsQuery(userId);
        var result = await _mediator.Send(query, ct);
        return result.ToActionResult();
    }
}