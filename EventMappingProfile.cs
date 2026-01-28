using AutoMapper;
using Calendar_Service.Contracts;
using Calendar_Service.Models;

namespace Calendar_Service;

public class EventMappingProfile : Profile
{
    public EventMappingProfile()
    {
        CreateMap<CreateEventRequest, CalendarEvent>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId));

        CreateMap<CalendarEvent, EventResponse>()
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.StartTime + src.Duration));
    }
}