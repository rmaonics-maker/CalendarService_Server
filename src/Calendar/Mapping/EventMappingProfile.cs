using AutoMapper;
using Calendar.Contracts.Requests;
using Calendar_Service.Models;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Responses;

namespace Calendar_Service.Mapping;

public class AppointmentMappingProfile : Profile
{
    public AppointmentMappingProfile()
    {
        CreateMap<CreateAppointmentRequest, AppointmentDto>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<AppointmentDto, Appointment>().ReverseMap();

        CreateMap<AppointmentDto, AppointmentResponse>();
    }
}