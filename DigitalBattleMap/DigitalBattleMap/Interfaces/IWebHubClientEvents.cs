using System;

namespace DigitalBattleMap.Interfaces;

public interface IWebHubClientEvents
{   
    event EventHandler<EventArgs> OnConnected;
    event EventHandler<EventArgs> OnDisconnect;
}