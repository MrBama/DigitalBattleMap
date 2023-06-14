using System;
using System.Collections.Generic;
using DigitalBattleMap.Common;

namespace DigitalBattleMap.DataClasses;

public class MoveTokenEventArgs : EventArgs
{
    public TokenIndentifier TokenIndentifier { get; set; } = new();
    public Direction Direction { get; set; }
}

public class ToggleConditionEventArgs : EventArgs
{
    public TokenIndentifier TokenIndentifier { get; set; } = new();
    public Condition Condition { get; set; }
}

public class ZLevelChangedEventArgs : EventArgs
{
    public ZLevelDirection ZLevelDirection { get; set; }
}

public class ConditionsChangedEventArgs : EventArgs
{
    public TokenIndentifier TokenIndentifier { get; set; } = new();
    public List<Condition> NewConditions { get; set; } = new();
}

public class GetConditionsEventArgs : EventArgs
{
    public TokenIndentifier TokenIndentifier { get; set; } = new();
}
