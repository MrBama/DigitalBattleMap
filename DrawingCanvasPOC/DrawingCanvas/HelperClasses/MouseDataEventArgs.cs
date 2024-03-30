using System.Windows;
using System.Windows.Input;

namespace DrawingCanvas.HelperClasses;

public class MouseDataEventArgs
{
    public MouseDataEventArgs(MouseEventArgs mouseEventArgs, Point position)
    {
        MouseEventArgs = mouseEventArgs;
        Position = position;
    }

    public MouseEventArgs MouseEventArgs { get; set; }
    public Point Position { get; set; }
}
