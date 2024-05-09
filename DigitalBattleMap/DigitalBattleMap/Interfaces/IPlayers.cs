using DigitalBattleMap.DataClasses;
using System;

namespace DigitalBattleMap.Interfaces;

public interface IPlayers
{
    event EventHandler<TokensOrientationChangedEventArgs> OnOrientationChanged;

    void AddTokenToPlayer(TokenIndentifier tokenIndentifier);
    bool IsTokenControlledByPlayer(TokenIndentifier tokenIndentifier);
    bool TryGetOrientation(TokenIndentifier tokenIndentifier, out TokenOrientation orientation);
}
