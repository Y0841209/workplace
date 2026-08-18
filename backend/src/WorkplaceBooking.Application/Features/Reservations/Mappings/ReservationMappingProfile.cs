using AutoMapper;
using WorkplaceBooking.Application.Features.Reservations.DTOs;
using WorkplaceBooking.Domain.Entities;

namespace WorkplaceBooking.Application.Features.Reservations.Mappings;

public class ReservationMappingProfile : Profile
{
    public ReservationMappingProfile()
    {
        CreateMap<Reservation, ReservationDto>()
            .ForMember(dest => dest.ResourceCode, opt => opt.MapFrom(src => src.Resource!.Code))
            .ForMember(dest => dest.ResourceName, opt => opt.MapFrom(src => src.Resource!.Name))
            .ForMember(dest => dest.ResourceTypeCode, opt => opt.MapFrom(src => src.Resource!.ResourceTypeCode))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User!.DisplayName))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User!.Email))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<CheckIn, CheckInDto>();
        CreateMap<AvailabilitySlot, AvailabilitySlotDto>();
    }
}