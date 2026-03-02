using AutoMapper;
using Microsoft.AspNetCore.Identity;
using ADMIN.Model;
using ADMIN.Data.Dto;

namespace USER.MAPPER
{
    public class MappingProfile : Profile 
    {
        public MappingProfile()
        {
            var hash = new PasswordHasher<object>();

            // RequestTable mappings
            CreateMap<RequestTable, RequestDetailDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RequestUserId, opt => opt.MapFrom(src => src.RequestUserId))
                .ForMember(dest => dest.VerifierId, opt => opt.MapFrom(src => src.VerifierId))
                .ForMember(dest => dest.VerifiedByAdmin, opt => opt.MapFrom(src => src.VerifiedByAdmin))
                .ForMember(dest => dest.HasRightToAdd, opt => opt.MapFrom(src => src.RightToAdd))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.VerifiedAt, opt => opt.MapFrom(src => src.VerifiedAt))
                .ForMember(dest => dest.RightsGrantedAt, opt => opt.MapFrom(src => src.RightsGrantedAt));

            CreateMap<CreateRequestDto, RequestTable>();
        }
    }
}
