using AutoMapper;

namespace Calendar_Service.Mapping
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<Calendar.Contracts.CreateUserRequest, Models.User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
            CreateMap<Models.User, Calendar.Contracts.UserResponse>();
        }
    }
}
