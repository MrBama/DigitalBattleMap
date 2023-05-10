using System;
using System.Collections.Generic;
using System.Windows.Documents;
using DigitalBattleMap.Common;

namespace DigitalBattleMap.DataClasses;

public class MoveTokenEventArgs : EventArgs
{
    public string Name { get; set; } = "";
    public Direction Direction { get; set; }
}

public class ToggleConditionEventArgs : EventArgs
{
    public string Name { get; set; } = "";
    public Condition Condition { get; set; }
}

public class ZLevelChangedEventArgs : EventArgs
{
    public ZLevelDirection ZLevelDirection { get; set; }
}

public class ConditionsChangedEventArgs : EventArgs
{
    public string Name { get; set; } = "";
    public List<Condition> NewConditions { get; set; } = new();
}

public class GetConditionsEventArgs : EventArgs
{
    public string Name { get; set; } = "";
}
