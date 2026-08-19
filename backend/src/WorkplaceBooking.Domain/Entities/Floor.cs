using WorkplaceBooking.SharedKernel.Primitives;
using WorkplaceBooking.SharedKernel.Results;

namespace WorkplaceBooking.Domain.Entities;

public class Floor : Entity, IAuditableEntity
{
    public Guid LocationId { get; private set; }
    public int FloorNumber { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool Active { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public Location? Location { get; private set; }

    private Floor() { }

    private Floor(Guid id, Guid locationId, int floorNumber, string code, string name)
        : base(id)
    {
        LocationId = locationId;
        FloorNumber = floorNumber;
        Code = code;
        Name = name;
        Active = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<Floor> Create(Guid locationId, int floorNumber, string code, string name)
    {
        if (locationId == Guid.Empty)
            return Result.Failure<Floor>(new Error("FLOOR_LOCATION_REQUIRED", "Location is required"));

        if (floorNumber <= 0)
            return Result.Failure<Floor>(new Error("FLOOR_NUMBER_INVALID", "Floor number must be positive"));

        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<Floor>(new Error("FLOOR_CODE_REQUIRED", "Floor code is required"));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Floor>(new Error("FLOOR_NAME_REQUIRED", "Floor name is required"));

        return Result.Success(new Floor(Guid.NewGuid(), locationId, floorNumber, code, name));
    }

    public void Update(string? code = null, string? name = null, bool? active = null)
    {
        if (code != null) Code = code;
        if (name != null) Name = name;
        if (active.HasValue) Active = active.Value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}