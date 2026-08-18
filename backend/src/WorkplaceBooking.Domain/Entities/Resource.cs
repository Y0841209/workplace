namespace WorkplaceBooking.Domain.Entities;

public class Resource : Entity, IAuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string ResourceTypeCode { get; private set; } = string.Empty;
    public Guid LocationId { get; private set; }
    public Guid FloorId { get; private set; }
    public Guid? ZoneId { get; private set; }
    public int Capacity { get; private set; }
    public Guid? PublicQrId { get; private set; }
    public int QrVersion { get; private set; }
    public bool Active { get; private set; }
    public bool Reservable { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Navigation
    public ResourceType? ResourceType { get; private set; }
    public Location? Location { get; private set; }
    public Floor? Floor { get; private set; }
    public Zone? Zone { get; private set; }

    private Resource() { }

    private Resource(Guid id, string code, string name, string resourceTypeCode, Guid locationId, Guid floorId, Guid? zoneId, int capacity, Guid? publicQrId)
        : base(id)
    {
        Code = code;
        Name = name;
        ResourceTypeCode = resourceTypeCode;
        LocationId = locationId;
        FloorId = floorId;
        ZoneId = zoneId;
        Capacity = capacity;
        PublicQrId = publicQrId;
        QrVersion = 1;
        Active = true;
        Reservable = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<Resource> Create(
        string code,
        string name,
        string resourceTypeCode,
        Guid locationId,
        Guid floorId,
        Guid? zoneId,
        int capacity,
        Guid? publicQrId)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure(new Error("RESOURCE_CODE_REQUIRED", "Resource code is required"));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(new Error("RESOURCE_NAME_REQUIRED", "Resource name is required"));

        if (string.IsNullOrWhiteSpace(resourceTypeCode))
            return Result.Failure(new Error("RESOURCE_TYPE_REQUIRED", "Resource type is required"));

        if (locationId == Guid.Empty)
            return Result.Failure(new Error("RESOURCE_LOCATION_REQUIRED", "Location is required"));

        if (floorId == Guid.Empty)
            return Result.Failure(new Error("RESOURCE_FLOOR_REQUIRED", "Floor is required"));

        if (capacity <= 0)
            return Result.Failure(new Error("RESOURCE_CAPACITY_INVALID", "Capacity must be positive"));

        // QR Policy validation
        var requiresQr = resourceTypeCode == "OPEN_WORKSPACE" || resourceTypeCode == "CLOSED_OFFICE";
        var forbidsQr = resourceTypeCode == "MEETING_ROOM";

        if (requiresQr && publicQrId == null)
            return Result.Failure(new Error("RESOURCE_QR_REQUIRED", "QR code is required for this resource type"));

        if (forbidsQr && publicQrId != null)
            return Result.Failure(new Error("RESOURCE_QR_FORBIDDEN", "QR code is not allowed for meeting rooms"));

        return Result.Success(new Resource(Guid.NewGuid(), code, name, resourceTypeCode, locationId, floorId, zoneId, capacity, publicQrId));
    }

    public void Update(
        string? name = null,
        string? resourceTypeCode = null,
        Guid? locationId = null,
        Guid? floorId = null,
        Guid? zoneId = null,
        int? capacity = null,
        Guid? publicQrId = null,
        bool? active = null,
        bool? reservable = null)
    {
        // Validate QR policy if resource type is changing
        var newTypeCode = resourceTypeCode ?? ResourceTypeCode;
        var newPublicQrId = publicQrId ?? PublicQrId;

        var requiresQr = newTypeCode == "OPEN_WORKSPACE" || newTypeCode == "CLOSED_OFFICE";
        var forbidsQr = newTypeCode == "MEETING_ROOM";

        if (requiresQr && newPublicQrId == null)
            throw new DomainException("QR code is required for this resource type", "RESOURCE_QR_REQUIRED");

        if (forbidsQr && newPublicQrId != null)
            throw new DomainException("QR code is not allowed for meeting rooms", "RESOURCE_QR_FORBIDDEN");

        if (name != null) Name = name;
        if (resourceTypeCode != null) ResourceTypeCode = resourceTypeCode;
        if (locationId.HasValue) LocationId = locationId.Value;
        if (floorId.HasValue) FloorId = floorId.Value;
        if (zoneId.HasValue) ZoneId = zoneId.Value;
        if (capacity.HasValue)
        {
            if (capacity <= 0)
                throw new DomainException("Capacity must be positive", "RESOURCE_CAPACITY_INVALID");
            Capacity = capacity.Value;
        }
        if (publicQrId.HasValue) PublicQrId = publicQrId.Value;
        if (active.HasValue) Active = active.Value;
        if (reservable.HasValue) Reservable = reservable.Value;

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RegenerateQr()
    {
        if (ResourceTypeCode == "MEETING_ROOM")
            throw new DomainException("Cannot generate QR for meeting rooms", "RESOURCE_QR_FORBIDDEN");

        PublicQrId = Guid.NewGuid();
        QrVersion++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}