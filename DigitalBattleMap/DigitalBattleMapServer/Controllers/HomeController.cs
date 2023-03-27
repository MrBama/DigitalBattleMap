using DigitalBattleMapServer.Application;
using DigitalBattleMapServer.Models;
using DigitalBattleMapServer.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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
