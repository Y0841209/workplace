namespace WorkplaceBooking.Domain.Entities;

public class AppUser : AggregateRoot
{
    public Guid EntraObjectId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? JobTitle { get; private set; }
    public string? Department { get; private set; }
    public bool Active { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private AppUser() { }

    private AppUser(Guid id, Guid entraObjectId, string email, string displayName)
        : base(id)
    {
        EntraObjectId = entraObjectId;
        Email = email;
        DisplayName = displayName;
        Active = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<AppUser> Create(Guid entraObjectId, string email, string displayName)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result.Failure(new Error("USER_EMAIL_REQUIRED", "Email is required"));

        if (string.IsNullOrWhiteSpace(displayName))
            return Result.Failure(new Error("USER_NAME_REQUIRED", "Display name is required"));

        return Result.Success(new AppUser(Guid.NewGuid(), entraObjectId, email, displayName));
    }
}