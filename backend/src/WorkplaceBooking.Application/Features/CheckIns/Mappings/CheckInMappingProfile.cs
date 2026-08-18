using AutoMapper;
using WorkplaceBooking.Application.Features.CheckIns.DTOs;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Application.Features.CheckIns.Mappings;

public class CheckInMappingProfile : Profile
{
    public CheckInMappingProfile()
    {
        CreateMap<CheckIn, CheckInDto>();
    }
}