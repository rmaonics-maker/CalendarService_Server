using AutoMapper;
using Calendar.Booking.API.Models;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Requests;
using NpgsqlTypes;

namespace Calendar.Booking.API.Mapping;

public class BookingMappingProfile : Profile
{
    public BookingMappingProfile()
    {
        // Request -> DTO
        CreateMap<CreateBookingRequest, BookingDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid())) 
            //.ForMember(dest => dest.UserId, opt => opt.Ignore()) 
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(_ => DateTime.UtcNow));
        
        // DTO - Entity
        CreateMap<BookingDto, BookingModel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Period, opt => opt.MapFrom(src => new NpgsqlRange<DateTime>(src.StartTimeUtc, src.EndTimeUtc)));
        
        //Entity - DTO
        CreateMap<BookingModel, BookingDto>()
            .ForMember(dest => dest.StartTimeUtc, opt => opt.MapFrom(src => src.Period.LowerBound))
            .ForMember(dest => dest.EndTimeUtc, opt => opt.MapFrom(src => src.Period.UpperBound));
    }
}