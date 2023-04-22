using DigitalBattleMap.Common;

namespace DigitalBattleMap.Interfaces;

public interface ITokenController
{
    public void MoveToken(string name, Direction direction);
    public void ToggleCondition(string name, Condition condition);
}
