using Microsoft.AspNetCore.Mvc;

namespace DigitalBattleMapServer.Components;

public class Menu : ViewComponent
{
    public Task<IViewComponentResult> InvokeAsync()
    {
        return Task.FromResult<IViewComponentResult>(View());
    }
}
