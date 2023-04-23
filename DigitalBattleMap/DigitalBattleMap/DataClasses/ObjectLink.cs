using System;

namespace DigitalBattleMap.DataClasses;
public class ObjectLink
{
    public Type LinkableObjectType { get; set; }
    public int Index { get; set; }
    public TokenIndentifier TokenIndentifier { get; set; }
}
