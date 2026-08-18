using Ardalis.Specification;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Domain.Specifications;

public class SingleAppSettingsSpec : Specification<AppSettings>
{
    public SingleAppSettingsSpec()
    {
        Query.Where(x => true); // Singleton
    }
}

public class ResourceByIdSpec : Specification<Resource>
{
    public ResourceByIdSpec(Guid resourceId)
    {
        Query.Where(r => r.Id == resourceId);
    }
}

public class ActiveExceptionForUserSpec : Specification<ReservationException>
{
    public ActiveExceptionForUserSpec(Guid userId, string? resourceTypeCode = null)
    {
        Query.Where(e => e.UserId == userId
            && e.Active
            && DateOnly.FromDateTime(DateTime.Today) >= e.ValidFrom
            && DateOnly.FromDateTime(DateTime.Today) <= e.ExpiresAt
            && (string.IsNullOrWhiteSpace(resourceTypeCode) || e.AppliesToResourceTypeCode == resourceTypeCode));
    }
}

public class ActiveRoleForUserSpec : Specification<UserApplicationRole>
{
    public ActiveRoleForUserSpec(Guid userId, string roleCode)
    {
        Query.Where(r => r.UserId == userId
            && r.RoleCode == roleCode
            && r.Active
            && DateOnly.FromDateTime(DateTime.Today) >= r.ValidFrom
            && (!r.ExpiresAt.HasValue || DateOnly.FromDateTime(DateTime.Today) <= r.ExpiresAt.Value));
    }
}

public class ActiveProfilesForUserSpec : Specification<UserBusinessProfile>
{
    public ActiveProfilesForUserSpec(Guid userId)
    {
        Query.Where(p => p.UserId == userId
            && p.Active
            && DateOnly.FromDateTime(DateTime.Today) >= p.ValidFrom
            && (!p.ExpiresAt.HasValue || DateOnly.FromDateTime(DateTime.Today) <= p.ExpiresAt.Value));
    }
}

public class PolicyForProfileAndTypeSpec : Specification<ResourceAccessPolicy>
{
    public PolicyForProfileAndTypeSpec(string profileCode, string resourceTypeCode)
    {
        Query.Where(p => p.BusinessProfileCode == profileCode
            && p.ResourceTypeCode == resourceTypeCode
            && p.Active);
    }
}

public class OverlappingReservationSpec : Specification<Reservation>
{
    public OverlappingReservationSpec(Guid resourceId, DateOnly date, TimeOnly startTime, TimeOnly endTime, Guid? excludeReservationId = null)
    {
        Query.Where(r => r.ResourceId == resourceId
            && r.ReservationDate == date
            && r.Status.In(ReservationStatus.CONFIRMED, ReservationStatus.CHECKED_IN)
            && r.StartTime < endTime
            && r.EndTime > startTime);

        if (excludeReservationId.HasValue)
            Query.Where(r => r.Id != excludeReservationId.Value);
    }
}