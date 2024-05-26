using DigitalBattleMap.Common;
using DigitalBattleMap.Common.Dto;
using DigitalBattleMapServer.Handlers;
using DigitalBattleMapServer.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace DigitalBattleMapServer.Controllers;

[Produces("application/json")]
public class MapController : Controller
{
    private readonly IHubContext<MapHub, IMapHub> _hubContext;
    private readonly IMemoryCacheHandler _memoryCacheHandler;

    public MapController(IHubContext<MapHub, IMapHub> hubContext, IMemoryCacheHandler memoryCacheHandler, IWebHostEnvironment hostEnvironment)
    {
        _hubContext = hubContext;
        _memoryCacheHandler = memoryCacheHandler;
    }
    
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Get(DrawLayer layer)
    {
        byte[] data = _memoryCacheHandler.Get(layer.ToString());
        if (data?.Any() == false)
            return NotFound();
        
        return File(data, "image/png");
    }

    [HttpPost]
    public async Task<IActionResult> Set([FromBody] MapUpdateDto mapUpdateDto)
    {
        _memoryCacheHandler.Set(mapUpdateDto.Layer.ToString(), mapUpdateDto.Data);
        await _hubContext.Clients.All.UpdateMap(mapUpdateDto.Layer);

        return Ok();
    }

    [HttpDelete]
    public IActionResult Delete(DrawLayer layer)
    {
        if (layer == DrawLayer.All)
        {
            _memoryCacheHandler.Delete(DrawLayer.All.ToString());
            _memoryCacheHandler.Delete(DrawLayer.Background.ToString());
            _memoryCacheHandler.Delete(DrawLayer.GridAndStrokes.ToString());
            _memoryCacheHandler.Delete(DrawLayer.Tokens.ToString());

            _hubContext.Clients.All.UpdateMap(DrawLayer.All);
        }
        else
        {
            _memoryCacheHandler.Delete(layer.ToString());
            _hubContext.Clients.All.UpdateMap(layer);
        }
        
        return Ok();
    }

    public IActionResult GetCharacterNavigationViewComponent(string uid)
    {
        return ViewComponent("CharacterNavigation");
    }
}