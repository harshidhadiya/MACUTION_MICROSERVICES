using AutoMapper;
using VERIFY.Model;
using VERIFY.Data.Dto;
using Microsoft.AspNetCore.Identity;
using Name;

namespace VERIFY.MAPPER
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            Tokenget token = new Tokenget();
            var hash = new PasswordHasher<object>();

            CreateMap<UserCreateDto, UserTable>()
                .ForMember(dest => dest.HashPassword, opt => opt.MapFrom(src => hash.HashPassword(new object(), src.Password)))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            CreateMap<UserTable, UserCreateDto>();
            CreateMap<UserTable, SignupResponceDto>().ForMember(x => x.token, opt => opt.MapFrom(x => token.getToken(x.Name, x.Role, x.Id.ToString()))).ForMember(x => x.requestobj, opt => opt.Ignore());

        }
    }
}
