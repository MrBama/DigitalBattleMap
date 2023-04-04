using DigitalBattleMap.Common;
using DigitalBattleMapServer.Handlers;
using Microsoft.AspNetCore.SignalR;

namespace DigitalBattleMapServer.Hubs;
public class MapHub : Hub<IMapHub>
{
    private readonly IMemoryCacheHandler _memoryCacheHandler;

    public MapHub(IMemoryCacheHandler memoryCacheHandler)
    {
        _memoryCacheHandler = memoryCacheHandler;
    }
}
