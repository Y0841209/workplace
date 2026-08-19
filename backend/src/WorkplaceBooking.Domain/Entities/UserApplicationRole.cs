using WorkplaceBooking.SharedKernel.Primitives;
using WorkplaceBooking.SharedKernel.Results;
using WorkplaceBooking.SharedKernel.Exceptions;

namespace WorkplaceBooking.Domain.Entities;

public class UserApplicationRole : Entity, IAuditableEntity
{
    public Guid UserId { get; private set; }
    public string RoleCode { get; private set; } = string.Empty;
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ExpiresAt { get; private set; }
    public bool Active { get; private set; }
    public Guid? AssignedByUserId { get; private set; }
    public string? AssignmentReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public AppUser? User { get; private set; }
    public ApplicationRole? Role { get; private set; }
    public AppUser? AssignedByUser { get; private set; }

    private UserApplicationRole() { }

    private UserApplicationRole(Guid id, Guid userId, string roleCode, DateOnly validFrom, DateOnly? expiresAt, Guid? assignedByUserId, string? assignmentReason)
        : base(id)
    {
        UserId = userId;
        RoleCode = roleCode;
        ValidFrom = validFrom;
        ExpiresAt = expiresAt;
        Active = true;
        AssignedByUserId = assignedByUserId;
        AssignmentReason = assignmentReason;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<UserApplicationRole> Create(
        Guid userId,
        string roleCode,
        DateOnly validFrom,
        DateOnly? expiresAt,
        Guid? assignedByUserId,
        string? assignmentReason)
    {
        if (userId == Guid.Empty)
            return Result.Failure<UserApplicationRole>(new Error("USER_ROLE_USER_REQUIRED", "User is required"));

        if (string.IsNullOrWhiteSpace(roleCode))
            return Result.Failure<UserApplicationRole>(new Error("USER_ROLE_CODE_REQUIRED", "Role code is required"));

        if (expiresAt.HasValue && expiresAt < validFrom)
            return Result.Failure<UserApplicationRole>(new Error("USER_ROLE_DATES_INVALID", "Expires date must be after valid from date"));

        return Result.Success(new UserApplicationRole(Guid.NewGuid(), userId, roleCode, validFrom, expiresAt, assignedByUserId, assignmentReason));
    }

    public void Update(DateOnly? validFrom = null, DateOnly? expiresAt = null, bool? active = null, string? assignmentReason = null)
    {
        if (validFrom.HasValue) ValidFrom = validFrom.Value;
        if (expiresAt.HasValue)
        {
            if (expiresAt < ValidFrom)
                throw new DomainException("Expires date must be after valid from date", "USER_ROLE_DATES_INVALID");
            ExpiresAt = expiresAt;
        }
        if (active.HasValue) Active = active.Value;
        if (assignmentReason != null) AssignmentReason = assignmentReason;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsActiveOn(DateOnly date) =>
        Active && date >= ValidFrom && (!ExpiresAt.HasValue || date <= ExpiresAt);
}