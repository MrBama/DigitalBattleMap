namespace DigitalBattleMap.DataClasses;

public static class TabIndex
{
    public const int Campaign = 0;
    public const int Background = 1;
    public const int Fog = 2;
    public const int Drawing = 3;
    public const int Tokens = 4;
}

public static class TabMapIndex
{
    public const int Map = 0;
    public const int Overview = 1;
    public const int Statblocks = 2;
}

public enum ArrowDirection
{
    Up,
    Down,
    Left,
    Right
}

public enum TokenSize
{
    Tiny,
    Small,
    Medium,
    Large,
    Huge,
    Gargantuan
}

public enum TokenOrientation
{
    North,
    East,
    South,
    West
}

public enum ZLevelDirection
{
    Back,
    Front
}

public enum DrawingButton
{
    Black,
    Red,
    Green,
    Blue,
    Eraser
}

public enum DrawingShapeType
{
    Rectangle,
    Circle,
    Cone,
    Line
}

public enum FogShapeType
{
    DrawPolygon,
    AngularPolygon,
    Rectangle,
    Circle,
    NGon
}

public enum MouseCanvasMode
{
    Click,
    RectangleSelection,
    PolygonSelection,
    FixedRatioRectangleSelection
}

public enum WebViewPageType
{
    Uri,
    Html
}

public enum CollectionChangedAction
{
    Add,
    Insert,
    Remove,
    Clear
}

public enum ControlType
{
    LMB,
    RMB,
    Wheel,
    Scroll,
    Ctrl,
    Alt,
    Up,
    Down,
    Click
}

public enum NType
{
    Triangle,
    Tetragon,
    Pentagon,
    Hexagon,
    Heptagon,
    Octagon
}

public enum BackgroundColor
{
    Black,
    White
}

public enum DrawingShapeCommandAction
{
    Add,
    Remove,
    Edit,
    Erase
}

public enum BitmapRotation
{
    Rotate0,
    Rotate90,
    Rotate180,
    Rotate270
}
