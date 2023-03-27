using AutoMapper;
using DigitalBattleMapServer.Application;
using DigitalBattleMapServer.Models;

namespace DigitalBattleMapServer.Utility;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Settings, SettingsViewModel>().ReverseMap();
    }
}