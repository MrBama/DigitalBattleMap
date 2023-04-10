namespace DigitalBattleMap.Common;

public interface IWebHub
{
    Task MoveToken(string character, Direction direction);
}