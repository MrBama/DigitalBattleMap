using System;
using DigitalBattleMap.Common;

namespace DigitalBattleMap.DataClasses
{
    public class MoveTokenActionEventArgs : EventArgs
    {
        public string Name { get; set; } = "";
        public Direction Direction { get; set; }
    }

    public class ToggleConditionActionEventArgs : EventArgs
    {
        public string Name { get; set; } = "";
        public Condition Condition { get; set; }
    }

    public class ZLevelChangedEventArgs : EventArgs
    {
        public ZLevelDirection ZLevelDirection { get; set; }
    }
}
