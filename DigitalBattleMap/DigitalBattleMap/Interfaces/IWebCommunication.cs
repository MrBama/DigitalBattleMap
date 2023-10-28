using DigitalBattleMap.DataClasses;
using System;

namespace DigitalBattleMap.Interfaces;

public delegate void MoveTokenEventHandler(object sender, MoveTokenEventArgs e);
public delegate void ToggleConditionEventHandler(object sender, ToggleConditionEventArgs e);
public delegate void GetConditionsEventHandler(object sender, GetConditionsEventArgs e);
public delegate void GetTokensEventHandler(object sender, GetTokensEventArgs e);

public interface IWebCommunication
{
    public event EventHandler<EventArgs> OnConnected;
    public event EventHandler<EventArgs> OnDisconnect;
    public event MoveTokenEventHandler OnMoveToken;
    public event ToggleConditionEventHandler OnToggleCondition;
    public event GetConditionsEventHandler OnGetConditions;
    public event GetTokensEventHandler OnGetTokens;

    public void SendMessage(IWebMessage webMessage);
}
