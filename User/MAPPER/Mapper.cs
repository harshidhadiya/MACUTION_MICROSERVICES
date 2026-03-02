using AutoMapper;
using USER.Model;
using USER.Data.Dto;
using Microsoft.AspNetCore.Identity;

namespace USER.MAPPER
{
    public class MappingProfile : Profile 
    {
        public MappingProfile()
        {
            var hash = new PasswordHasher<object>();

            CreateMap<UserCreateDto, UserTable>()
                .ForMember(dest => dest.HashPassword, opt => opt.MapFrom(src => hash.HashPassword(new object(), src.Password)))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            CreateMap<UserTable, UserCreateDto>();
        }
    }
}
