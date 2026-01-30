using AutoMapper;

namespace Calendar_Service.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<Contracts.CreateUserRequest, Models.User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<Models.User, Contracts.UserResponse>();
        }
    }
}
