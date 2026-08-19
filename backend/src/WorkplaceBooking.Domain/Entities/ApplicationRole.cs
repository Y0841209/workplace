using WorkplaceBooking.SharedKernel.Primitives;
using WorkplaceBooking.SharedKernel.Results;

namespace WorkplaceBooking.Domain.Entities;

public class ApplicationRole : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool Active { get; private set; }

    private ApplicationRole() { }

    private ApplicationRole(string code, string name, string? description)
        : base(Guid.NewGuid())
    {
        Code = code;
        Name = name;
        Description = description;
        Active = true;
    }

    public static Result<ApplicationRole> Create(string code, string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<ApplicationRole>(new Error("ROLE_CODE_REQUIRED", "Role code is required"));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<ApplicationRole>(new Error("ROLE_NAME_REQUIRED", "Role name is required"));

        return Result.Success(new ApplicationRole(code, name, description));
    }

    public void Update(string? name = null, string? description = null, bool? active = null)
    {
        if (name != null) Name = name;
        if (description != null) Description = description;
        if (active.HasValue) Active = active.Value;
    }
}