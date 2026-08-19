using WorkplaceBooking.SharedKernel.Primitives;
using WorkplaceBooking.SharedKernel.Results;

namespace WorkplaceBooking.Domain.Entities;

public class ResourceType : Entity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool QrRequired { get; private set; }
    public bool CheckinRequired { get; private set; }
    public bool Active { get; private set; }

    private ResourceType() { }

    private ResourceType(string code, string name, bool qrRequired, bool checkinRequired)
        : base(Guid.NewGuid())
    {
        Code = code;
        Name = name;
        QrRequired = qrRequired;
        CheckinRequired = checkinRequired;
        Active = true;
    }

    public static Result<ResourceType> Create(string code, string name, bool qrRequired, bool checkinRequired)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<ResourceType>(new Error("RESOURCE_TYPE_CODE_REQUIRED", "Resource type code is required"));

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<ResourceType>(new Error("RESOURCE_TYPE_NAME_REQUIRED", "Resource type name is required"));

        return Result.Success(new ResourceType(code, name, qrRequired, checkinRequired));
    }

    public void Update(string? name = null, bool? qrRequired = null, bool? checkinRequired = null, bool? active = null)
    {
        if (name != null) Name = name;
        if (qrRequired.HasValue) QrRequired = qrRequired.Value;
        if (checkinRequired.HasValue) CheckinRequired = checkinRequired.Value;
        if (active.HasValue) Active = active.Value;
    }
}