using DigitalBattleMap.DataClasses;

namespace DigitalBattleMap.Interfaces;

public delegate void MoveTokenEventHandler(object sender, MoveTokenEventArgs e);
public delegate void ToggleConditionEventHandler(object sender, ToggleConditionEventArgs e);
public delegate void GetConditionsEventHandler(object sender, GetConditionsEventArgs e);

public interface IWebCommunication
{
    public event MoveTokenEventHandler OnMoveToken;
    public event ToggleConditionEventHandler OnToggleCondition;
    public event GetConditionsEventHandler OnGetConditions;

    public void SendMessage(IWebMessage webMessage);
}
