using AutoMapper;
using Calendar.Contracts;
using Calendar_Service.Models;

namespace Calendar_Service.Mapping;

public class EventMappingProfile : Profile
{
    public EventMappingProfile()
    {
        CreateMap<CreateEventRequest, CalendarEvent>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
            .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.Duration))
            .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location))
            .ForMember(dest => dest.RecurrentRule, opt => opt.MapFrom(src => src.RecurrentRule))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId));

        CreateMap<CalendarEvent, EventResponse>()
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.StartTime + src.Duration));
    }
}