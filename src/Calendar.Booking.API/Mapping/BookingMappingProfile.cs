using AutoMapper;
using Calendar.Booking.API.Models;
using Calendar.Contracts.Dtos;
using NpgsqlTypes;

namespace Calendar.Booking.API.Mapping;

public class BookingMappingProfile : Profile
{
    public BookingMappingProfile()
    {
        // DTO - Entity
        CreateMap<BookingDto, BookingModel>().ForMember(dest => dest.Id, opt => opt.Ignore())
        .ForMember(dest => dest.Period, opt => opt.MapFrom(src => new NpgsqlRange<DateTime>(src.StartTimeUtc, src.EndTimeUtc)));
        
        //Entity - DTO
        CreateMap<BookingModel, BookingDto>()
            .ForMember(dest => dest.StartTimeUtc, opt => opt.MapFrom(src => src.Period.LowerBound))
            .ForMember(dest => dest.EndTimeUtc, opt => opt.MapFrom(src => src.Period.UpperBound));
    }
}