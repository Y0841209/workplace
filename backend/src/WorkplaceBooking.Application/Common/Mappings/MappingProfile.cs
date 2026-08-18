using AutoMapper;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Domain entities to DTOs mappings will be added in feature-specific mapping profiles
        // This base profile can contain shared mappings if needed
    }
}