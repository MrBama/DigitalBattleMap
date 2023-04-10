using DigitalBattleMap.Common;
using System;

namespace DigitalBattleMap.Events;

public class MoveTokenActionEventArgs : EventArgs
{
    public string Name { get; init; }
    public Direction Direction { get; init; }
}
