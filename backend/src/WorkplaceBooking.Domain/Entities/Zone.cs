namespace WorkplaceBooking.Domain.Entities;

public class Zone : Entity, IAuditableEntity
{
    public Guid FloorId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool Active { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public Floor? Floor { get; private set; }

    private Zone() { }

    private Zone(Guid id, Guid floorId, string code, string name)
        : base(id)
    {
        FloorId = floorId;
        Code = code;
        Name = name;
        Active = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<Zone> Create(Guid floorId, string code, string name)
    {
        if (floorId == Guid.Empty)
            return Result.Failure(new Error("ZONE_FLOOR_REQUIRED", "Floor is required"));

        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure(new Error("ZONE_CODE_REQUIRED", "Zone code is required"));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(new Error("ZONE_NAME_REQUIRED", "Zone name is required"));

        return Result.Success(new Zone(Guid.NewGuid(), floorId, code, name));
    }

    public void Update(string? code = null, string? name = null, bool? active = null)
    {
        if (code != null) Code = code;
        if (name != null) Name = name;
        if (active.HasValue) Active = active.Value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}