using System;
using System.Windows;

namespace DrawingCanvas;

public class StrokeDrawingShape : DrawingShape
{
    public StrokeDrawingShape(IMouse mouse, Action applyShapeCallback) : base(mouse, applyShapeCallback)
    {
    }

    public override bool IsErasable => true;

    protected override void ButtonDown(Point position)
    {
        Points.Add(position);
    }

    protected override void ButtonUp(Point position)
    {
        ApplyShape();
    }

    protected override void MouseMove(Point position, bool buttonDown)
    {
        if (buttonDown)
        {
            if (!Points.Contains(position))
            {
                Points.Add(position);
                BitmapTools.SmoothLine(Points, Size);
            }
        }
    }
}
