using AutoMapper;
using Calendar_Service.Models;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Requests;
using Calendar.Contracts.Responses;
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
                if (context.TryGetItems(out var items) && items.TryGetValue("OccurrenceDate", out var obj) && obj is Instant date)
                    return $"{src.Id}_{date:yyyyMMddTHHmmss'Z'}";
                return src.Id.ToString();
            }))
            .ForMember(d => d.MasterId, opt => opt.MapFrom(s => s.Id))
            .ForMember(d => d.IsException, opt => opt.MapFrom(_ => false))
            .ForMember(d => d.StartTime, opt => opt.MapFrom((src, dest, _, context) =>
            {
                if (context.TryGetItems(out var items) && items.TryGetValue("OccurrenceDate", out var obj) && obj is Instant date)
                    return date.ToDateTimeOffset();
                return src.Slot.Start.ToDateTimeOffset();
            }))
            .ForMember(d => d.Duration, opt => opt.MapFrom(s =>
                (s.Slot.End - s.Slot.Start).ToTimeSpan()))
            .ForMember(d => d.RecurrentRule, opt => opt.MapFrom(s => s.Recurrence))
            .ForMember(d => d.CreatedAt, opt => opt.MapFrom(s => (DateTimeOffset?)s.CreatedAt));

        CreateMap<RecurrencePattern, RecurrencePatternDto>();
        CreateMap<RecurrencePatternDto, RecurrencePattern>();

        CreateMap<CreateAppointmentRequest, AppointmentDto>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.MasterId, opt => opt.Ignore())
            .ForMember(d => d.IsException, opt => opt.Ignore())
            .ForMember(d => d.RecurrentRule, opt => opt.MapFrom(s =>
                s.RecurrentRule != null
                    ? new RecurrencePatternDto(s.RecurrentRule, null, null)
                    : null))
            .ForMember(d => d.UserId, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore());

        CreateMap<AppointmentDto, Appointment>()
            .ForMember(d => d.Id, opt => opt.MapFrom((src, _, _) =>
                src.Id != null && Guid.TryParse(src.Id, out var id) && id != Guid.Empty
                    ? id
                    : Guid.NewGuid()))
            .ForMember(d => d.Slot, opt => opt.MapFrom(s => new TimeSlot(
                Instant.FromDateTimeOffset(s.StartTime),
                Instant.FromDateTimeOffset(s.StartTime).Plus(Duration.FromTimeSpan(s.Duration)))))
            .ForMember(d => d.Recurrence, opt => opt.MapFrom(s => s.RecurrentRule))
            .ForMember(d => d.DateTimeZoneId, opt => opt.MapFrom(_ => "UTC"))
            .ForMember(d => d.Version, opt => opt.Ignore())
            .ForMember(d => d.CreatedAt, opt => opt.Ignore());

        CreateMap<AppointmentDto, AppointmentResponse>()
            .ConvertUsing((src, _) => new AppointmentResponse(
                Guid.TryParse(src.Id, out var id) ? id : Guid.Empty,
                src.Title ?? string.Empty,
                src.Description ?? string.Empty,
                src.StartTime,
                src.Duration,
                src.Location ?? string.Empty,
                src.RecurrentRule != null ? src.RecurrentRule.RRule : null,
                src.UserId,
                src.CreatedAt?.UtcDateTime ?? DateTime.UtcNow));
    }
}
