using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WorkplaceBooking.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? DisplayName { get; }
    IEnumerable<string> Roles { get; }
    IEnumerable<string> BusinessProfiles { get; }
    bool IsInRole(string role);
    bool HasBusinessProfile(string profile);
    bool CanReserveResource(string resourceTypeCode);
}