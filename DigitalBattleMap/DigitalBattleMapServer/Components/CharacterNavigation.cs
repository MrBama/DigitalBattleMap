using DigitalBattleMap.Common;
using DigitalBattleMapServer.Application;
using DigitalBattleMapServer.Models;
using DigitalBattleMapServer.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DigitalBattleMapServer.Components;

public class CharacterNavigation : ViewComponent
{
    private readonly IState<Settings> _settingsState;

    public CharacterNavigation(IState<Settings> settingsState)
    {
        _settingsState = settingsState;
    }

    public Task<IViewComponentResult> InvokeAsync()
    {
        Settings settings = _settingsState.Get();

        NavigationComponentViewModel componentViewModel = new()
        {
            Orientation = settings?.Orientation ?? Orientation.Down
        };

        return Task.FromResult<IViewComponentResult>(View(componentViewModel));
    }
}