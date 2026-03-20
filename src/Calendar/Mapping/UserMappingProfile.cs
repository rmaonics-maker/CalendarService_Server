using AutoMapper;
using Calendar.Contracts.Requests;
using Calendar.Contracts.Responses;

namespace Calendar_Service.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<CreateUserRequest, Models.User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<Models.User, UserResponse>();
        }
    }
}
