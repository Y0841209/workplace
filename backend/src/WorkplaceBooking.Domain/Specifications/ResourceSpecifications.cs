using Ardalis.Specification;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Domain.Specifications;

public class ResourceByCodeSpec : Specification<Resource>
{
    public ResourceByCodeSpec(string code)
    {
        Query.Where(r => r.Code == code);
    }
}

public class ResourceTypeByCodeSpec : Specification<ResourceType>
{
    public ResourceTypeByCodeSpec(string code)
    {
        Query.Where(rt => rt.Code == code);
    }
}

public class ActiveResourceTypesSpec : Specification<ResourceType>
{
    public ActiveResourceTypesSpec()
    {
        Query.Where(rt => rt.Active);
    }
}

public class ResourcesFilteredSpec : Specification<Resource>
{
    public ResourcesFilteredSpec(
        string? resourceTypeCode = null,
        Guid? floorId = null,
        Guid? zoneId = null,
        bool? active = null,
        bool? reservable = null,
        string? search = null)
    {
        Query.Where(r => true);

        if (!string.IsNullOrWhiteSpace(resourceTypeCode))
            Query.Where(r => r.ResourceTypeCode == resourceTypeCode);

        if (floorId.HasValue)
            Query.Where(r => r.FloorId == floorId.Value);

        if (zoneId.HasValue)
            Query.Where(r => r.ZoneId == zoneId.Value);

        if (active.HasValue)
            Query.Where(r => r.Active == active.Value);

        if (reservable.HasValue)
            Query.Where(r => r.Reservable == reservable.Value);

        if (!string.IsNullOrWhiteSpace(search))
            Query.Where(r => r.Code.Contains(search) || r.Name.Contains(search));
    }
}

public class ResourcesByFloorSpec : Specification<Resource>
{
    public ResourcesByFloorSpec(
        Guid floorId,
        string? resourceTypeCode = null,
        bool? active = null,
        bool? reservable = null)
    {
        Query.Where(r => r.FloorId == floorId && r.Active && r.Reservable);

        if (!string.IsNullOrWhiteSpace(resourceTypeCode))
            Query.Where(r => r.ResourceTypeCode == resourceTypeCode);

        if (active.HasValue)
            Query.Where(r => r.Active == active.Value);

        if (reservable.HasValue)
            Query.Where(r => r.Reservable == reservable.Value);
    }
}

public class AvailableResourcesSpec : Specification<Resource>
{
    public AvailableResourcesSpec(
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        string? resourceTypeCode = null,
        Guid? floorId = null,
        Guid? zoneId = null,
        int? minCapacity = null)
    {
        Query.Where(r => r.Active && r.Reservable);

        if (!string.IsNullOrWhiteSpace(resourceTypeCode))
            Query.Where(r => r.ResourceTypeCode == resourceTypeCode);

        if (floorId.HasValue)
            Query.Where(r => r.FloorId == floorId.Value);

        if (zoneId.HasValue)
            Query.Where(r => r.ZoneId == zoneId.Value);

        if (minCapacity.HasValue)
            Query.Where(r => r.Capacity >= minCapacity.Value);
    }
}

public class MeetingRoomsSpec : Specification<Resource>
{
    public MeetingRoomsSpec(
        Guid? floorId = null,
        int? minCapacity = null,
        bool? active = null)
    {
        Query.Where(r => r.ResourceTypeCode == "MEETING_ROOM" && r.Active && r.Reservable);

        if (floorId.HasValue)
            Query.Where(r => r.FloorId == floorId.Value);

        if (minCapacity.HasValue)
            Query.Where(r => r.Capacity >= minCapacity.Value);

        if (active.HasValue)
            Query.Where(r => r.Active == active.Value);
    }
}