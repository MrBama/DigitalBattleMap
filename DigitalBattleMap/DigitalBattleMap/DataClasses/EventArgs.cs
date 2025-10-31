using DigitalBattleMap.Common;
using DigitalBattleMap.DrawingShapes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Input;

namespace DigitalBattleMap.DataClasses;

public class MoveTokenEventArgs : EventArgs
{
    public TokenIdentifier TokenIdentifier { get; set; } = new();
    public Direction Direction { get; set; }
}

public class ToggleConditionEventArgs : EventArgs
{
    public TokenIdentifier TokenIdentifier { get; set; } = new();
    public Condition Condition { get; set; }
}

public class ZLevelChangedEventArgs : EventArgs
{
    public ZLevelDirection ZLevelDirection { get; set; }
}

public class ConditionsChangedEventArgs : EventArgs
{
    public TokenIdentifier TokenIdentifier { get; set; } = new();
    public List<Condition> NewConditions { get; set; } = new();
}

public class GetConditionsEventArgs : EventArgs
{
    public TokenIdentifier TokenIdentifier { get; set; } = new();
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

public class SetOrientationEventArgs : EventArgs
{
    public string Player { get; set; } = "";
    public Orientation Orientation { get; set; }
}

public class GridSizeChangedEventArgs : EventArgs
{
    public int NewGridSize { get; set; }
}

public class GridSizeZoomAndEnhanceEventArgs : EventArgs
{
    public RectangleF rectangle { get; set; }
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
    public Point<double> Position { get; set; } = new();
}

public class MouseButtonDataEventArgs : EventArgs
{
    public Point<double> Position { get; set; } = new();
}

public class MouseWheelDataEventArgs : EventArgs
{
    public Point<double> Position { get; set; } = new();
    public int Delta { get; set; }
}

public class MouseMoveDataEventArgs : EventArgs
{
    public Point<double> Position { get; set; } = new();
    public bool LeftButtonDown { get; set; }
    public bool RightButtonDown { get; set; }
}

public class TokensOrientationChangedEventArgs : EventArgs
{
    public List<TokenIdentifier> TokenIdentifiers { get; set; } = new();
    public TokenOrientation Orientation { get; set; }
}

public class SetHeightEventArgs : EventArgs
{
    public TokenIdentifier TokenIdentifier { get; set; } = new();
    public int Height { get; set; }
}
