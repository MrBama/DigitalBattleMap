using System;

namespace DigitalBattleMap;

public interface IWebHubClientEvents
{
    delegate void MoveTokenActionEventHandler(object sender, MoveTokenActionEventArgs e);
    
    event EventHandler<EventArgs> OnConnected;
    event EventHandler<EventArgs> OnDisconnect;
    event MoveTokenActionEventHandler OnMoveToken;
}