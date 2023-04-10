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
    
    public async Task<IViewComponentResult> InvokeAsync()
    {
        Settings settings = _settingsState.Get();
        
        // TODO: Write tag helper for IEnumerable<T> to List<SelectListItem>
        List<SelectListItem> selectListItems = new ();
        if (!string.IsNullOrWhiteSpace(settings?.Characters))
            selectListItems.AddRange(settings.Characters.Split(',').Select(value => new SelectListItem(value, value)));
        
        NavigationComponentViewModel componentViewModel = new NavigationComponentViewModel
        {
            Orientation = settings?.Orientation ?? Orientation.Down,
            Characters = selectListItems
        };
    
        return View(componentViewModel);
    }
}