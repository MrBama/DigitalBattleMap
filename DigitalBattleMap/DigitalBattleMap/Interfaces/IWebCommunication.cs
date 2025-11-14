using DigitalBattleMap.DataClasses;
using System;

namespace DigitalBattleMap.Interfaces;

public interface IWebCommunication
{
    public event EventHandler<EventArgs> OnConnected;
    public event EventHandler<DisconnectedEventArgs> OnDisconnect;
    public event EventHandler<MoveTokenEventArgs> OnMoveToken;
    public event EventHandler<ToggleConditionEventArgs> OnToggleCondition;
    public event EventHandler<GetConditionsEventArgs> OnGetConditions;
    public event EventHandler<SetOrientationEventArgs> OnSetOrientation;
    public event EventHandler<SetHeightEventArgs> OnSetHeight;

    public void SendMessage(IWebMessage webMessage);
}
