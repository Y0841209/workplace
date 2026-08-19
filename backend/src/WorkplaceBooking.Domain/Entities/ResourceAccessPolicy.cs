using WorkplaceBooking.SharedKernel.Primitives;
using WorkplaceBooking.SharedKernel.Results;

namespace WorkplaceBooking.Domain.Entities;

public class ResourceAccessPolicy : Entity, IAuditableEntity
{
    public string ResourceTypeCode { get; private set; } = string.Empty;
    public string BusinessProfileCode { get; private set; } = string.Empty;
    public bool CanView { get; private set; }
    public bool CanReserve { get; private set; }
    public bool CanModifyOwn { get; private set; }
    public bool Active { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public ResourceType? ResourceType { get; private set; }
    public BusinessProfile? BusinessProfile { get; private set; }

    private ResourceAccessPolicy() { }

    private ResourceAccessPolicy(Guid id, string resourceTypeCode, string businessProfileCode, bool canView, bool canReserve, bool canModifyOwn)
        : base(id)
    {
        ResourceTypeCode = resourceTypeCode;
        BusinessProfileCode = businessProfileCode;
        CanView = canView;
        CanReserve = canReserve;
        CanModifyOwn = canModifyOwn;
        Active = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<ResourceAccessPolicy> Create(
        string resourceTypeCode,
        string businessProfileCode,
        bool canView,
        bool canReserve,
        bool canModifyOwn)
    {
        if (string.IsNullOrWhiteSpace(resourceTypeCode))
            return Result.Failure<ResourceAccessPolicy>(new Error("ACCESS_POLICY_RESOURCE_TYPE_REQUIRED", "Resource type is required"));

        if (string.IsNullOrWhiteSpace(businessProfileCode))
            return Result.Failure<ResourceAccessPolicy>(new Error("ACCESS_POLICY_PROFILE_REQUIRED", "Business profile is required"));

        return Result.Success(new ResourceAccessPolicy(Guid.NewGuid(), resourceTypeCode, businessProfileCode, canView, canReserve, canModifyOwn));
    }

    public void Update(bool? canView = null, bool? canReserve = null, bool? canModifyOwn = null, bool? active = null)
    {
        if (canView.HasValue) CanView = canView.Value;
        if (canReserve.HasValue) CanReserve = canReserve.Value;
        if (canModifyOwn.HasValue) CanModifyOwn = canModifyOwn.Value;
        if (active.HasValue) Active = active.Value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}