using DigitalBattleMap.Utilities;
using System.Collections.Generic;

namespace DigitalBattleMap.DataClasses;

public class TokenMoveCommand
{
    public TokenMoveCommand(TokenIndentifier tokenIndentifier, Point<int> offset) : this(new List<TokenIndentifier> { tokenIndentifier }, offset)
    {
    }

    public TokenMoveCommand(List<TokenIndentifier> tokenIndentifiers, Point<int> offset)
    {
        TokenIndentifiers = new List<TokenIndentifier>(tokenIndentifiers.Clone());
        Offset = offset;
    }

    public List<TokenIndentifier> TokenIndentifiers { get; set; } = new();
    public Point<int> Offset { get; set; }
}
