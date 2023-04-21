using System;

namespace DigitalBattleMap.DataClasses;

public interface IWebHubClientEvents
{
    delegate void MoveTokenActionEventHandler(object sender, MoveTokenActionEventArgs e);
    delegate void ToggleConditionActionEventHandler(object sender, ToggleConditionActionEventArgs e);
    
    event EventHandler<EventArgs> OnConnected;
    event EventHandler<EventArgs> OnDisconnect;
    event MoveTokenActionEventHandler OnMoveToken;
}