using DigitalBattleMap.Common;
using DigitalBattleMap.Common.Dto;
using DigitalBattleMapServer.Application;
using DigitalBattleMapServer.Handlers;
using DigitalBattleMapServer.Hubs;
using DigitalBattleMapServer.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Text.Json;

namespace DigitalBattleMapServer.Controllers;

[Produces("application/json")]
public class NavigationController : Controller
{
    private readonly IHubContext<WebHub, IWebHub> _webHub;
    private readonly IState<Settings> _settings;
    private readonly IMemoryCacheHandler _memoryCacheHandler;

    public NavigationController(IHubContext<WebHub, IWebHub> webHub, IMemoryCacheHandler memoryCacheHandler, IState<Settings> settings)
    {
        _webHub = webHub;
        _memoryCacheHandler = memoryCacheHandler;
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

        if (settings.Name != null && settings.Name != "")
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
    public IActionResult SetHeight(string character, int height)
    {
        _webHub.Clients.All.SetHeight(character, height);
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
        var campaign = _memoryCacheHandler.Get<Dictionary<string, List<string>>>(CacheKeys.Campaign);
        if(campaign != null)
        {
            campaign[tokensDto.Player.ToLower()] = tokensDto.Tokens;
            _memoryCacheHandler.Set(CacheKeys.Campaign, campaign);
        }

        _webHub.Clients.All.SetTokens(tokensDto.Player, tokensDto.Tokens);
        return Ok();
    }

    [HttpPost]
    public IActionResult SetCampaign([FromBody] CampaignDto campaignDto)
    {
        Dictionary<string, List<string>> players = new();
        foreach (var player in campaignDto.Players)
        {
            players[player.Key.ToLower()] = player.Value;
        }
        _memoryCacheHandler.Set(CacheKeys.Campaign, players);
        _webHub.Clients.All.SetCampaign(players);
        return Ok();
    }

    [HttpPost]
    public IActionResult GetConditions(string character)
    {
        _webHub.Clients.All.GetConditions(character);
        return Ok();
    }

    [HttpPost]
    public IActionResult ClearCache()
    {
        _memoryCacheHandler.Clear();
        return Ok();
    }

    [HttpGet]
    public IActionResult GetTokens(string player)
    {
        var campaign = _memoryCacheHandler.Get<Dictionary<string, List<string>>>(CacheKeys.Campaign);
        if (campaign != null)
        {
            if (campaign.TryGetValue(player.ToLower(), out var tokens))
            {
                string json = JsonSerializer.Serialize(tokens);
                return Content(json);
            }
        }

        return Ok();
    }

    private Direction TranslateDirection(Orientation orientation, Direction direction)
    {
        int maxValue = Enum.GetValues(typeof(Direction)).Cast<int>().Max();





        return Direction.North;
    }
}