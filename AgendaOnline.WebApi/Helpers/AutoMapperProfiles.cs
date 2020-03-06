using System.Linq;
using AutoMapper;
using AgendaOnline.Domain;
using AgendaOnline.Domain.Identity;
using AgendaOnline.WebApi.Dtos;

namespace AgendaOnline.WebApi.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<User, UserDto>().ReverseMap();
            CreateMap<User, UserLoginDto>().ReverseMap();
            CreateMap<Agenda, AgendaDto>().ReverseMap();
        }
    }
}