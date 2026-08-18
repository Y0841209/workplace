using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Application.Features.Users.DTOs;

public record UserDto(
    Guid Id,
    Guid EntraObjectId,
    string Email,
    string DisplayName,
    string? JobTitle,
    string? Department,
    bool Active,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> BusinessProfiles,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record AssignProfileDto(
    Guid UserId,
    string ProfileCode,
    DateOnly ValidFrom,
    DateOnly? ExpiresAt,
    string? AssignmentReason);

public record AssignRoleDto(
    Guid UserId,
    string RoleCode,
    DateOnly ValidFrom,
    DateOnly? ExpiresAt,
    string? AssignmentReason);

public record CreateExceptionDto(
    Guid UserId,
    int MaximumFutureActiveReservations,
    string? AppliesToResourceTypeCode,
    DateOnly ValidFrom,
    DateOnly ExpiresAt,
    string Reason);

public record UserProfileDto(
    Guid Id,
    string UserId,
    string ProfileCode,
    string ProfileName,
    DateOnly ValidFrom,
    DateOnly? ExpiresAt,
    bool Active);

public record UserRoleDto(
    Guid Id,
    string UserId,
    string RoleCode,
    string RoleName,
    DateOnly ValidFrom,
    DateOnly? ExpiresAt,
    bool Active);

public record ExceptionDto(
    Guid Id,
    Guid UserId,
    int MaximumFutureActiveReservations,
    string? AppliesToResourceTypeCode,
    DateOnly ValidFrom,
    DateOnly ExpiresAt,
    string Reason,
    bool Active);