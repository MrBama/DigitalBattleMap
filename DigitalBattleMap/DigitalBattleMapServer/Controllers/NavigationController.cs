using DigitalBattleMap.Common;
using DigitalBattleMap.Common.Dto;
using DigitalBattleMapServer.Application;
using DigitalBattleMapServer.Hubs;
using DigitalBattleMapServer.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DigitalBattleMapServer.Controllers;

[Produces("application/json")]
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
    public IActionResult ChangeOrientation()
    {
        var settings = _settings.Get();

        settings.Orientation++;
        if ((int)settings.Orientation > Enum.GetValues<Orientation>().Length - 1)
        {
            settings.Orientation = 0;
        }
        _settings.Set(settings);

        if(settings.Name != null && settings.Name != "")
        {
            _webHub.Clients.All.SetOrientation(settings.Name, settings.Orientation);
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult SetOrientation()
    {
        var settings = _settings.Get();
        if (settings.Name != null && settings.Name != "")
        {
            _webHub.Clients.All.SetOrientation(settings.Name, settings.Orientation);
        }

        return Ok();
    }

    [HttpPost]
    public IActionResult ToggleCondition(string character, Condition condition)
    {
        _webHub.Clients.All.ToggleCondition(character, condition);
        return Ok();
    }

    [HttpPost]
    public IActionResult SetConditions([FromBody] ConditionsDto conditionsDto)
    {
        _webHub.Clients.All.SetConditions(conditionsDto.Character, conditionsDto.Conditions);
        return Ok();
    }

    [HttpPost]
    public IActionResult SetTokens([FromBody] TokensDto tokensDto)
    {
        _webHub.Clients.All.SetTokens(tokensDto.Player, tokensDto.Tokens);
        return Ok();
    }

    [HttpPost]
    public IActionResult SetCampaign([FromBody] CampaignDto campaignDto)
    {
        _webHub.Clients.All.SetCampaign(campaignDto.Players);
        return Ok();
    }

    [HttpPost]
    public IActionResult GetTokens(string player)
    {
        _webHub.Clients.All.GetTokens(player);
        return Ok();
    }

    [HttpPost]
    public IActionResult GetConditions(string character)
    {
        _webHub.Clients.All.GetConditions(character);
        return Ok();
    }

    private Direction TranslateDirection(Orientation orientation, Direction direction)
    {
        int maxValue = Enum.GetValues(typeof(Direction)).Cast<int>().Max();





        return Direction.North;
    }
}