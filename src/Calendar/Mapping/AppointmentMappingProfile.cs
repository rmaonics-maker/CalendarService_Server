using AutoMapper;
using Calendar.Contracts.Requests;
using Calendar_Service.Models;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Responses;
using NpgsqlTypes;

namespace Calendar_Service.Mapping;

public class AppointmentMappingProfile : Profile
{
    public AppointmentMappingProfile()
    {
        CreateMap<CreateAppointmentRequest, AppointmentDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.Empty))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(_ => Guid.Empty))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => (DateTime?)null));

        CreateMap<AppointmentDto, Appointment>()
            .ForMember(dest => dest.Period, opt => opt.MapFrom(src =>
                new NpgsqlRange<DateTime>(
                    src.StartTime.UtcDateTime,
                    src.StartTime.UtcDateTime + src.Duration)))
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(src =>
                src.CreatedAt ?? DateTime.UtcNow))
            .ForMember(dest => dest.Version, opt => opt.Ignore());

        CreateMap<Appointment, AppointmentDto>()
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src =>
                new DateTimeOffset(src.Period.LowerBound, TimeSpan.Zero)))
            .ForMember(dest => dest.Duration, opt => opt.MapFrom(src =>
                src.Period.UpperBound - src.Period.LowerBound))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAtUtc));

        CreateMap<AppointmentDto, AppointmentResponse>();
    }
}