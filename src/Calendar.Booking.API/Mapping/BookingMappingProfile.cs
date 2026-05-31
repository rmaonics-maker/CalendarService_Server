using AutoMapper;
using Calendar.Booking.API.Models;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Requests;
using NpgsqlTypes;

using Calendar.Contracts.Responses;

namespace Calendar.Booking.API.Mapping;

public class BookingMappingProfile : Profile
{
    public BookingMappingProfile()
    {
        // Request -> DTO
        CreateMap<CreateBookingRequest, BookingDto>()
            .ConstructUsing(src => new BookingDto(
                Guid.NewGuid(),
                src.Title,
                src.RoomId,
                src.StartTimeUtc,
                src.EndTimeUtc,
                DateTimeOffset.UtcNow,
                src.AppointmentId
            ))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore());
        
        // DTO -> Response
        CreateMap<BookingDto, BookingResponse>()
            .ConstructUsing(src => new BookingResponse(
                src.Id,
                src.Title,
                Guid.Empty, 
                src.RoomId,
                src.StartTimeUtc,
                src.EndTimeUtc,
                src.CreatedAtUtc,
                src.AppointmentId
            ))
            .ForMember(dest => dest.UserId, opt => opt.Ignore());


        // DTO - Entity

        CreateMap<BookingDto, BookingModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAtUtc))
            .ForMember(dest => dest.Version, opt => opt.Ignore())
            .ForMember(dest => dest.AppointmentId, opt => opt.MapFrom(src => src.AppointmentId))
            .ForMember(dest => dest.Period, opt => opt.MapFrom(src => new NpgsqlRange<DateTime>(src.StartTimeUtc.UtcDateTime, src.EndTimeUtc.UtcDateTime)));
        
        //Entity - DTO
        CreateMap<BookingModel, BookingDto>()
            .ConstructUsing(src => new BookingDto(
                src.Id,
                src.Title ?? string.Empty,
                src.RoomId,
                src.Period.LowerBound,
                src.Period.UpperBound,
                src.CreatedAt,
                src.AppointmentId
            ))
            .ForMember(dest => dest.StartTimeUtc, opt => opt.Ignore())
            .ForMember(dest => dest.EndTimeUtc, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore());
    }
}