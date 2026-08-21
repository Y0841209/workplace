using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Linq;
using WorkplaceBooking.Application.Common.Interfaces;

namespace WorkplaceBooking.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? _httpContextAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(userIdClaim, out var guid) ? guid : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value
                          ?? _httpContextAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

    public string? DisplayName => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value
                               ?? _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value;

    public IEnumerable<string> Roles => _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value) ?? Enumerable.Empty<string>();

    public IEnumerable<string> BusinessProfiles => _httpContextAccessor.HttpContext?.User?.FindAll("business_profile")?.Select(c => c.Value) ?? Enumerable.Empty<string>();

    public bool IsInRole(string role) => _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;

    public bool HasBusinessProfile(string profile) => _httpContextAccessor.HttpContext?.User?.FindAll("business_profile").Any(c => c.Value == profile) ?? false;

    public bool CanReserveResource(string resourceTypeCode)
    {
        if (IsInRole("GLOBAL_ADMIN")) return true;
        if (IsInRole("ROOM_ADMIN") && resourceTypeCode == "MEETING_ROOM") return true;

        var profiles = BusinessProfiles;
        var policyMatrix = new Dictionary<string, Dictionary<string, bool>>
        {
            ["COLLABORATOR"] = new() { ["OPEN_WORKSPACE"] = true, ["CLOSED_OFFICE"] = false, ["MEETING_ROOM"] = true },
            ["ASSOCIATE"] = new() { ["OPEN_WORKSPACE"] = true, ["CLOSED_OFFICE"] = false, ["MEETING_ROOM"] = true },
            ["LEADER"] = new() { ["OPEN_WORKSPACE"] = true, ["CLOSED_OFFICE"] = true, ["MEETING_ROOM"] = true },
            ["DIRECTOR"] = new() { ["OPEN_WORKSPACE"] = true, ["CLOSED_OFFICE"] = true, ["MEETING_ROOM"] = true },
            ["PARTNER"] = new() { ["OPEN_WORKSPACE"] = true, ["CLOSED_OFFICE"] = true, ["MEETING_ROOM"] = true }
        };

        foreach (var profile in BusinessProfiles)
        {
            if (policyMatrix.TryGetValue(profile, out var policy) && policy.TryGetValue(resourceTypeCode, out var canReserve) && canReserve)
                return true;
        }
        return false;
    }
}