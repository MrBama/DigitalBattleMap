using DigitalBattleMapServer.Application;
using DigitalBattleMapServer.Utility;
using Microsoft.AspNetCore.Mvc;

namespace DigitalBattleMapServer.Controllers;
public class HomeController : Controller
{
    private readonly IState<Settings> _settingsState;

    public HomeController(IState<Settings> settingsState)
    {
        _settingsState = settingsState;
    }

    [HttpGet]
    [Route("")]
    public IActionResult Index()
    {
        return View();
    }
}
