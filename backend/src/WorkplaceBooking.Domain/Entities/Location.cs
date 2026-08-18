namespace WorkplaceBooking.Domain.Entities;

public class Location : Entity, IAuditableEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string Timezone { get; private set; } = string.Empty;
    public bool Active { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Location() { }

    private Location(Guid id, string code, string name, string city, string country, string timezone)
        : base(id)
    {
        Code = code;
        Name = name;
        City = city;
        Country = country;
        Timezone = timezone;
        Active = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<Location> Create(string code, string name, string city, string country, string timezone)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure(new Error("LOCATION_CODE_REQUIRED", "Location code is required"));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(new Error("LOCATION_NAME_REQUIRED", "Location name is required"));

        return Result.Success(new Location(Guid.NewGuid(), code, name, city, country, timezone));
    }

    public void Update(string? name = null, string? city = null, string? country = null, string? timezone = null, bool? active = null)
    {
        if (name != null) Name = name;
        if (city != null) City = city;
        if (country != null) Country = country;
        if (timezone != null) Timezone = timezone;
        if (active.HasValue) Active = active.Value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}