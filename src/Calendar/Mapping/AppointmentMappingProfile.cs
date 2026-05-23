using AutoMapper;
using Calendar_Service.Models;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Types;
using NodaTime;

namespace Calendar_Service.Mapping;

public class AppointmentMappingProfile : Profile
{
    public AppointmentMappingProfile()
    {
        CreateMap<Appointment, AppointmentDto>()
            .ForMember(d => d.Id, opt => opt.MapFrom((src, dest, _, context) =>
            {
                if (context.Items.TryGetValue("OccurrenceDate", out var obj) && obj is Instant date)
                    return $"{src.Id}_{date:yyyyMMddTHHmmss'Z'}";
                return src.Id.ToString();
            }))
            .ForMember(d => d.MasterId, opt => opt.MapFrom(s => s.Id))
            .ForMember(d => d.IsException, opt => opt.MapFrom(_ => false))
            .ForMember(d => d.StartTime, opt => opt.MapFrom((src, dest, _, context) =>
            {
                if (context.Items.TryGetValue("OccurrenceDate", out var obj) && obj is Instant date)
                    return date.ToDateTimeOffset();
                return src.Slot.Start.ToDateTimeOffset();
            }))
            .ForMember(d => d.Duration, opt => opt.MapFrom(s => 
                (s.Slot.End - s.Slot.Start).ToTimeSpan()))
            .ForMember(d => d.RecurrentRule, opt => opt.MapFrom(s => s.Recurrence))
            .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => (DateTimeOffset?)s.CreatedAt));

        CreateMap<RecurrencePattern, RecurrencePatternDto>();
    }
}
