namespace DigitalBattleMap.Common;

public interface IWebHub
{
    Task MoveToken(string character, Direction direction);
    Task ToggleCondition(string character, Condition condition);
    Task SetConditions(string character, List<Condition> conditions);
}