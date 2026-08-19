using WorkplaceBooking.SharedKernel.Primitives;
using WorkplaceBooking.SharedKernel.Results;

namespace WorkplaceBooking.Domain.Entities;

public class BusinessProfile : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool Active { get; private set; }

    private BusinessProfile() { }

    private BusinessProfile(string code, string name)
        : base(Guid.NewGuid())
    {
        Code = code;
        Name = name;
        Active = true;
    }

    public static Result<BusinessProfile> Create(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<BusinessProfile>(new Error("PROFILE_CODE_REQUIRED", "Profile code is required"));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<BusinessProfile>(new Error("PROFILE_NAME_REQUIRED", "Profile name is required"));

        return Result.Success(new BusinessProfile(code, name));
    }

    public void Update(string? name = null, bool? active = null)
    {
        if (name != null) Name = name;
        if (active.HasValue) Active = active.Value;
    }
}