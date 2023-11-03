using DigitalBattleMap.DataClasses;

namespace DigitalBattleMap.Interfaces;

public interface IPlayers
{
    void AddTokenToPlayer(TokenIndentifier tokenIndentifier);
    bool IsTokenControlledByPlayer(TokenIndentifier tokenIndentifier);
}
