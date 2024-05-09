using DigitalBattleMap.DataClasses;
using System;

namespace DigitalBattleMap.Interfaces;

public interface IPlayers
{
    event EventHandler<TokensOrientationChangedEventArgs> OnOrientationChanged;

    void AddTokenToPlayer(TokenIdentifier tokenIdentifier);
    bool IsTokenControlledByPlayer(TokenIdentifier tokenIdentifier);
    bool TryGetOrientation(TokenIdentifier tokenIdentifier, out TokenOrientation orientation);
}
