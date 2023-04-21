using DigitalBattleMap.Common;
using DigitalBattleMapServer.Application;
using DigitalBattleMapServer.Hubs;
using DigitalBattleMapServer.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DigitalBattleMapServer.Controllers;

public class NavigationController : Controller
{
    private readonly IHubContext<WebHub, IWebHub> _webHub;
    private readonly IState<Settings> _settings;
    
    public NavigationController(IHubContext<WebHub, IWebHub> webHub, IState<Settings> settings)
    {
        _webHub = webHub;
        _settings = settings;
    }
    
    [HttpPost]
    public IActionResult Move(string character, Direction direction)
    {
        _webHub.Clients.All.MoveToken(character, direction.GetOrientatedDirection(_settings.Get().Orientation));
        return Ok();
        
    }

    [HttpPost]
    public IActionResult ToggleCondition(string character, Condition condition)
    {
        _webHub.Clients.All.ToggleCondition(character, condition);
        return Ok();
    }

    private Direction TranslateDirection(Orientation orientation, Direction direction)
    {
        int maxValue = Enum.GetValues(typeof(Direction)).Cast<int>().Max();


        
        
        
        return Direction.North;
    }
}