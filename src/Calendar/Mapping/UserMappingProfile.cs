using AutoMapper;
using Calendar_Service.Models;
using Calendar.Contracts.Dtos;
using Calendar.Contracts.Events;
using Calendar.Contracts.Requests;
using Calendar.Contracts.Responses;

namespace Calendar_Service.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<CreateUserRequest, UserDto>();

            CreateMap<UserRegisteredEvent, UserDto>();
            
            CreateMap<UpdateUserRequest, UserDto>();

            CreateMap<UserDto, UserResponse>();
            
            CreateMap<UserDto, User>();
        }
    }
}
