namespace DigitalBattleMap.DataClasses;

public class TokenOffset
{
    public TokenOffset(TokenIndentifier tokenIndentifier, Point<int> offset)
    {
        TokenIndentifier = tokenIndentifier;
        Offset = offset;
    }

    public TokenIndentifier TokenIndentifier { get; set; }
    public Point<int> Offset { get; set; }
}
