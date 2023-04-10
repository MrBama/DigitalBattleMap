using System;
using DigitalBattleMap.Common;

namespace DigitalBattleMap.DataClasses
{
    public class MoveTokenActionEventArgs : EventArgs
    {
        public string Name { get; set; } = "";
        public int Id { get; set; } = 1;
        public Direction Direction { get; set; }
    }

    public class ZLevelChangedEventArgs : EventArgs
    {
        public ZLevelDirection ZLevelDirection { get; set; }
    }
}
