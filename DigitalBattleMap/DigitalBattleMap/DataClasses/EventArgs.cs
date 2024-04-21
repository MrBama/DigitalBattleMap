using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using DigitalBattleMap.Common;
using DigitalBattleMap.DrawingShapes;

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

public class SetOrientationEventArgs : EventArgs
{
    public TokenIndentifier TokenIndentifier { get; set; } = new();
    public Orientation Orientation { get; set; }
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

public class CanvasSizeChangedEventArgs : EventArgs
{
    public Size<double> OldSize { get; set; } = new();
    public Size<double> NewSize { get; set; } = new();
}

public class GetTokensEventArgs : EventArgs
{
    public string Player { get; set; } = "";
}

public class GridSizeChangedEventArgs : EventArgs
{
    public int NewGridSize { get; set; }
}

public class DisconnectedEventArgs : EventArgs
{
    public bool IsConnectionLost { get; set; }
}

public class SettingChangedEventArgs : EventArgs
{
    public string SettingName { get; set; }
}

public class DrawingShapeCollectionChangedEventArgs : EventArgs
{
    public DrawingShape ChangedShape { get; set; }
    public CollectionChangedAction Action { get; set; }
    public int Index { get; set; }
}

public class MouseDataEventArgs : EventArgs
{
    public MouseEventArgs MouseEventArgs { get; set; }
    public Point<double> Position { get; set; } = new ();
}

public class MouseButtonDataEventArgs : EventArgs
{
    public Point<double> Position { get; set; } = new();
}

public class MouseMoveDataEventArgs : EventArgs
{
    public Point<double> Position { get; set; } = new();
    public bool LeftButtonDown { get; set; }
    public bool RightButtonDown { get; set; }
}
