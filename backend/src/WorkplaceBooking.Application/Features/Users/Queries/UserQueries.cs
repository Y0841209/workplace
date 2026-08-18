using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Features.Users.DTOs;

namespace WorkplaceBooking.Application.Features.Users.Queries;

public record GetCurrentUserQuery : IRequest<Result<UserDto>>;

public record GetUserByIdQuery(
    Guid UserId) : IRequest<Result<UserDto>>;

public record GetUserProfilesQuery(
    Guid UserId) : IRequest<Result<IReadOnlyList<UserProfileDto>>>;

public record GetUserRolesQuery(
    Guid UserId) : IRequest<Result<IReadOnlyList<UserRoleDto>>>;

public record GetUserExceptionsQuery(
    Guid UserId) : IRequest<Result<IReadOnlyList<ExceptionDto>>>;