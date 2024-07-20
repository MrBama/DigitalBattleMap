namespace DigitalBattleMap.DataClasses;

public static class TabIndex
{
    public const int Campaign = 0;
    public const int Background = 1;
    public const int Drawing = 2;
    public const int Tokens = 3;
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
