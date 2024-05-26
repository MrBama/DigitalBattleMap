using DigitalBattleMap.Utilities;
using System.Collections.Generic;

namespace DigitalBattleMap.DataClasses;

public class TokenMoveCommand
{
    public TokenMoveCommand(TokenIdentifier tokenIdentifier, Point<int> offset) : this(new List<TokenIdentifier> { tokenIdentifier }, offset)
    {
    }

    public TokenMoveCommand(List<TokenIdentifier> tokenIdentifiers, Point<int> offset)
    {
        TokenIdentifiers = new List<TokenIdentifier>(tokenIdentifiers.Clone());
        Offset = offset;
    }

    public List<TokenIdentifier> TokenIdentifiers { get; set; } = new();
    public Point<int> Offset { get; set; }
}
