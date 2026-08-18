using Ardalis.Result;
using MediatR;
using WorkplaceBooking.Application.Features.Users.DTOs;

namespace WorkplaceBooking.Application.Features.Users.Commands;

public record AssignProfileCommand(
    Guid UserId,
    string ProfileCode,
    DateOnly ValidFrom,
    DateOnly? ExpiresAt,
    string? AssignmentReason) : IRequest<Result<UserProfileDto>>;

public record AssignRoleCommand(
    Guid UserId,
    string RoleCode,
    DateOnly ValidFrom,
    DateOnly? ExpiresAt,
    string? AssignmentReason) : IRequest<Result<UserRoleDto>>;

public record CreateExceptionCommand(
    Guid UserId,
    int MaximumFutureActiveReservations,
    string? AppliesToResourceTypeCode,
    DateOnly ValidFrom,
    DateOnly ExpiresAt,
    string Reason) : IRequest<Result<ExceptionDto>>;

public record UpdateProfileCommand(
    Guid ProfileId,
    DateOnly? ValidFrom = null,
    DateOnly? ExpiresAt = null,
    bool? Active = null,
    string? AssignmentReason = null) : IRequest<Result>;

public record UpdateRoleCommand(
    Guid RoleId,
    DateOnly? ValidFrom = null,
    DateOnly? ExpiresAt = null,
    bool? Active = null,
    string? AssignmentReason = null) : IRequest<Result>;

public record UpdateExceptionCommand(
    Guid ExceptionId,
    int? MaximumFutureActiveReservations = null,
    DateOnly? ValidFrom = null,
    DateOnly? ExpiresAt = null,
    string? Reason = null,
    bool? Active = null) : IRequest<Result>;