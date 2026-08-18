using AutoMapper;
using WorkplaceBooking.Application.Features.Resources.DTOs;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Application.Features.Resources.Mappings;

public class ResourceMappingProfile : Profile
{
    public ResourceMappingProfile()
    {
        CreateMap<Resource, ResourceDto>()
            .ForMember(dest => dest.ResourceTypeName, opt => opt.MapFrom(src => src.ResourceType!.Name))
            .ForMember(dest => dest.LocationName, opt => opt.MapFrom(src => src.Location!.Name))
            .ForMember(dest => dest.FloorCode, opt => opt.MapFrom(src => src.Floor!.Code))
            .ForMember(dest => dest.ZoneName, opt => opt.MapFrom(src => src.Zone!.Name));

        CreateMap<ResourceType, ResourceTypeDto>();
    }
}